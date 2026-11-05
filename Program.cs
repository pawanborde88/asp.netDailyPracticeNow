using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using dailypracticeASPDotNet.Data;
using dailypracticeASPDotNet.Mappings;
using dailypracticeASPDotNet.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. CONTROLLERS CONFIGURATION
// ==========================================
builder.Services.AddControllers();

// ==========================================
// 2. DATABASE CONTEXT CONFIGURATION
// ==========================================
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

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
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not set in appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

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
        Title = "Daily Practice ASP.NET Core Web API",
        Version = "v1",
        Description = "ASP.NET Core Web API with add_user, fetch_users, login API, JWT Authentication, EF Core Migrations, Repository Pattern, and AutoMapper"
    });

    // Add JWT Bearer definition to Swagger
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
// 8. AUTOMATIC MIGRATIONS ON STARTUP (FOR DOCKER & DEPLOYMENT)
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Asynchronously apply any pending database migrations
        await db.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogWarning(ex, "Could not apply automatic database migrations at startup.");
    }
}

// ==========================================
// 9. HTTP REQUEST PIPELINE
// ==========================================
// Enable Swagger in Development or whenever configured (ideal for Docker deployments)
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Daily Practice API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI directly at http://localhost:port/
    });
}

app.UseHttpsRedirection();

// Authentication MUST be placed before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
