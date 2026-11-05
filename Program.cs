using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using dailypracticeASPDotNet.Data;
using dailypracticeASPDotNet.Mappings;
using dailypracticeASPDotNet.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Dynamic port binding for Cloud Platforms (Render, Railway, Fly.io, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ==========================================
// 1. CONTROLLERS CONFIGURATION
// ==========================================
builder.Services.AddControllers();

// ==========================================
// 2. DATABASE CONTEXT CONFIGURATION
// ==========================================
// Retrieves connection string (SQLite "dailypractice.db" in production/Render, SQL Server in local Development)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=dailypractice.db";

bool useSqlite = connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase)
              || connectionString.Contains("sqlite", StringComparison.OrdinalIgnoreCase)
              || (!OperatingSystem.IsWindows() && connectionString.Contains("DESKTOP-", StringComparison.OrdinalIgnoreCase));

if (useSqlite)
{
    var sqliteConnection = connectionString.Contains(".db", StringComparison.OrdinalIgnoreCase)
        ? connectionString
        : "Data Source=dailypractice.db";

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlite(sqliteConnection);
    });
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
    {
        options.UseSqlServer(connectionString);
    });
}

// ==========================================
// 3. REPOSITORY PATTERN DEPENDENCY INJECTION
// ==========================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();

// ==========================================
// 4. AUTOMAPPER REGISTRATION
// ==========================================
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<AutoMapperProfiles>());

// ==========================================
// 5. JWT AUTHENTICATION CONFIGURATION
// ==========================================
var jwtKey = builder.Configuration["Jwt:Key"] ?? "DailyPracticeSecretSuperKey1234567890!@#$%^&*()_+";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "DailyPracticeASPDotNet";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "DailyPracticeASPDotNetUsers";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

// ==========================================
// 6. AUTHORIZATION
// ==========================================
builder.Services.AddAuthorization();

// ==========================================
// 7. SWAGGER / OPENAPI WITH JWT AUTHORIZATION
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Daily Practice Web API [Cloud Ready]",
        Version = "v1",
        Description = "ASP.NET Core Web API with add_user, fetch_users, login API, JWT Authentication, EF Core, Repository Pattern, and AutoMapper"
    });

    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        Description = "Enter 'Bearer' [space] and then your valid JWT token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOi...\""
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme),
            new List<string>()
        }
    });
});

var app = builder.Build();

// ==========================================
// 8. REVERSE PROXY SUPPORT (RENDER / DOCKER)
// ==========================================
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// ==========================================
// 9. DATABASE INITIALIZATION & MIGRATIONS
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsSqlite())
        {
            // For SQLite (used seamlessly on Render/containers), EnsureCreated creates tables and seeds roles
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("SQLite database initialized successfully.");
        }
        else
        {
            // For SQL Server, apply EF Core migrations
            await db.Database.MigrateAsync();
            logger.LogInformation("SQL Server database migrated successfully.");
        }
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not apply automatic database migrations at startup.");
    }
}

// ==========================================
// 10. HTTP REQUEST PIPELINE
// ==========================================
// Serve Swagger UI on Development or in Container Deployments
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Daily Practice API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI directly at root
    });
}

// Authentication MUST be placed before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
