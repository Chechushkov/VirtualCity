using AutoMapper;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Extensions;
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
var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<Program>>();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

// Configure database, services, and authentication
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

// Configure MinIO client (separate from MinioService registration)
builder.Services.AddMinio(configureClient => configureClient
    .WithEndpoint(builder.Configuration["Minio:Endpoint"] ?? "localhost:9000")
    .WithCredentials(
        builder.Configuration["Minio:AccessKey"] ?? "minioadmin",
        builder.Configuration["Minio:SecretKey"] ?? "minioadmin")
    .WithSSL(false));



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

// Apply database migrations BEFORE any other database operations
logger.LogInformation("=== Starting database initialization ===");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var scopeLogger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // First, ensure database exists and apply migrations
        scopeLogger.LogInformation("Step 1: Checking database connection...");
        bool canConnect = await context.Database.CanConnectAsync();
        scopeLogger.LogInformation($"Database connection: {(canConnect ? "OK" : "Cannot connect")}");

        scopeLogger.LogInformation("Step 2: Applying database migrations...");
        try
        {
            // Get pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                scopeLogger.LogInformation($"Found {pendingMigrations.Count()} pending migrations:");
                foreach (var migration in pendingMigrations)
                {
                    scopeLogger.LogInformation($"  - {migration}");
                }

                // Apply migrations
                await context.Database.MigrateAsync();
                scopeLogger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                scopeLogger.LogInformation("No pending migrations found");
            }
        }
        catch (Exception migrateEx)
        {
            scopeLogger.LogError(migrateEx, "Failed to apply database migrations");
            throw;
        }

        scopeLogger.LogInformation("Step 3: Seeding initial data...");

        // Try to seed using DataSeeder first
        try
        {
            var dataSeeder = services.GetRequiredService<DataSeeder>();
            await dataSeeder.SeedAsync();
            scopeLogger.LogInformation("DataSeeder completed successfully");
        }
        catch (Exception ex)
        {
            scopeLogger.LogError(ex, "DataSeeder failed, creating users directly");

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
                scopeLogger.LogInformation("Created admin user");
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
                scopeLogger.LogInformation("Created creator user");
            }

            await context.SaveChangesAsync();
        }

        scopeLogger.LogInformation("=== Database initialization completed successfully ===");
    }
    catch (Exception ex)
    {
        scopeLogger.LogError(ex, "=== Database initialization FAILED ===");
        // Don't rethrow - let the application start even if seeding fails
        // but log the error clearly
    }
}

app.Run();
