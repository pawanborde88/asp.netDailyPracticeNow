using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using dailypracticeASPDotNet.Data;
using dailypracticeASPDotNet.DTOs;
using dailypracticeASPDotNet.Mappings;
using dailypracticeASPDotNet.Models;
using dailypracticeASPDotNet.Repositories;
using dailypracticeASPDotNet.Services;

var builder = WebApplication.CreateBuilder(args);

// Dynamic port binding for Cloud Platforms (Render, Railway, Fly.io, etc.)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// ==========================================
// 1. CONTROLLERS & STANDARDIZED API RESPONSE
// ==========================================
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(x => x.ErrorMessage).ToArray()
                );

            var apiResponse = new ApiResponse<object>
            {
                Success = false,
                Message = "One or more validation errors occurred.",
                Data = null,
                Errors = errors
            };

            return new BadRequestObjectResult(apiResponse);
        };
    });

// ==========================================
// CORS (Cross-Origin Resource Sharing)
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// 2. DATABASE CONTEXT CONFIGURATION
// ==========================================
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
// 3. REPOSITORY & SERVICES DEPENDENCY INJECTION
// ==========================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITokenRepository, TokenRepository>();
builder.Services.AddScoped<IPhotoService, PhotoService>();

// In-Memory Cache & OTP Service for passwordless OTP login
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IOtpService, OtpService>();

// Zoho CPaaS / ZeptoMail Transactional Email Service
builder.Services.Configure<ZeptoMailSettings>(builder.Configuration.GetSection(ZeptoMailSettings.SectionName));
builder.Services.AddHttpClient<IZeptoMailService, ZeptoMailService>();

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
        Description = "ASP.NET Core Web API with Full User CRUD APIs (Numeric Digit User IDs, Binary Photo Upload, Edit, Delete, Fetch All), Direct Binary Photo Stream, Zoho CPaaS / ZeptoMail OTP Login, Password Login, JWT Authentication, and Repository Pattern."
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
// 9. ENSURE WWWROOT & UPLOADS DIRECTORY EXISTS
// ==========================================
var webRootPath = app.Environment.WebRootPath;
if (string.IsNullOrWhiteSpace(webRootPath))
{
    webRootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
}
var uploadsDirectory = Path.Combine(webRootPath, "uploads", "user-photos");
if (!Directory.Exists(uploadsDirectory))
{
    Directory.CreateDirectory(uploadsDirectory);
}

// ==========================================
// 10. DATABASE INITIALIZATION & MIGRATIONS
// ==========================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        if (db.Database.IsSqlite())
        {
            // For SQLite, check if existing table has legacy Guid (TEXT) primary key and recreate if needed
            try
            {
                await db.Database.OpenConnectionAsync();
                using var command = db.Database.GetDbConnection().CreateCommand();
                command.CommandText = "PRAGMA table_info(Users);";
                using var reader = await command.ExecuteReaderAsync();
                bool idIsText = false;
                while (await reader.ReadAsync())
                {
                    var colName = reader["name"]?.ToString();
                    var colType = reader["type"]?.ToString();
                    if (string.Equals(colName, "Id", StringComparison.OrdinalIgnoreCase) &&
                        colType?.Contains("TEXT", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        idIsText = true;
                        break;
                    }
                }
                await reader.CloseAsync();

                if (idIsText)
                {
                    logger.LogWarning("Detected legacy GUID primary key in SQLite Users table. Recreating Users table with numeric Integer primary key...");
                    await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS Users;");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Notice during SQLite table inspect.");
            }

            await db.Database.EnsureCreatedAsync();

            // Safe backward-compatible column checks
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"PhotoUrl\" TEXT NULL;"); } catch { }
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"PhotoData\" BLOB NULL;"); } catch { }
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"PhotoContentType\" TEXT NULL;"); } catch { }
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Users\" ADD COLUMN \"PhotoFileName\" TEXT NULL;"); } catch { }

            logger.LogInformation("SQLite database initialized successfully with numeric digit user IDs (1, 2, 3...).");
        }
        else
        {
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
// 11. STATIC FILES SERVING (USER PHOTOS)
// ==========================================
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRootPath),
    RequestPath = ""
});

// ==========================================
// 12. HTTP REQUEST PIPELINE
// ==========================================
if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("EnableSwagger", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Daily Practice API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI directly at root
    });
}

// Enable CORS before Authentication & Authorization to handle browser OPTIONS preflight
app.UseCors("AllowAll");

// Authentication MUST be placed before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
