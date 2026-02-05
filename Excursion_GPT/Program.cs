using AutoMapper;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Minio;
using Excursion_GPT.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Excursion GPT API", Version = "v1" });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Configure database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure JWT authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var issuer = jwtSettings["Issuer"] ?? "ExcursionGPTApi";
var audience = jwtSettings["Audience"] ?? "ExcursionGPTClients";

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
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Configure MinIO
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
        builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
    .WithSSL(false));

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<IModelService, ModelService>();
builder.Services.AddScoped<ITrackService, TrackService>();
builder.Services.AddScoped<IPointService, PointService>();
builder.Services.AddScoped<IMinioService, MinioService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

// Register data seeders
builder.Services.AddScoped<BuildingDataSeeder>();
builder.Services.AddScoped<DataSeeder>();

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(Program).Assembly);

// Add health checks
builder.Services.AddHealthChecks();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // Try to seed using DataSeeder first
        try
        {
            var dataSeeder = services.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync();
            logger.LogInformation("DataSeeder completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DataSeeder failed, creating users directly");

            // Fallback: create users directly
            var adminUser = await context.Users.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            if (adminUser == null)
            {
                adminUser = new Excursion_GPT.Domain.Entities.User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = "Admin User",
                    Login = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminpass"),
                    Phone = "+1234567890",
                    SchoolName = "Admin Academy",
                    Role = Excursion_GPT.Domain.Enums.Role.Admin
                };
                await context.Users.AddAsync(adminUser);
                logger.LogInformation("Created admin user");
            }

            var creatorUser = await context.Users.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"));
            if (creatorUser == null)
            {
                creatorUser = new Excursion_GPT.Domain.Entities.User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "Creator User",
                    Login = "creator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("creatorpass"),
                    Phone = "+0987654321",
                    SchoolName = "Art School",
                    Role = Excursion_GPT.Domain.Enums.Role.Creator
                };
                await context.Users.AddAsync(creatorUser);
                logger.LogInformation("Created creator user");
            }

            await context.SaveChangesAsync();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
