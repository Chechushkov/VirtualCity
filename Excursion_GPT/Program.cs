using Excursion_GPT.Extensions;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Minio;
using Excursion_GPT.Middleware;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Custom extension methods for DI and configuration
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddSwaggerDocumentation();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Excursion GPT API V1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app root
        c.DisplayRequestDuration();
        c.EnableDeepLinking();
        c.EnableFilter();
    });
}
else
{
    // Production error handling
    app.UseExceptionHandler("/error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// Custom error handling middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

app.UseRouting();

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

// Initialize MinIO bucket and apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Apply database migrations
        app.Logger.LogInformation("Checking for pending database migrations...");

        try
        {
            // Force apply migrations even if there are pending model changes
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                app.Logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
                await context.Database.MigrateAsync();
                app.Logger.LogInformation("Database migrations applied successfully");
            }
            else
            {
                app.Logger.LogInformation("No pending database migrations");
            }
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("pending changes"))
        {
            // If migration fails due to pending changes, force create the database
            app.Logger.LogWarning("Migration failed due to pending changes. Creating database from scratch...");
            await context.Database.EnsureCreatedAsync();
            app.Logger.LogInformation("Database created successfully");
        }

        // Initialize MinIO bucket
        var minioService = services.GetRequiredService<IMinioService>();
        var minioBucketName = builder.Configuration["Minio:BucketName"] ?? "excursion-gpt-bucket";

        app.Logger.LogInformation("Initializing MinIO bucket: {BucketName}", minioBucketName);
        await minioService.CreateBucketIfNotExistAsync(minioBucketName);
        app.Logger.LogInformation("MinIO bucket initialized successfully");

        // Seed database with test data
        var dataSeeder = services.GetRequiredService<DataSeeder>();
        await dataSeeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database or initializing MinIO.");

        // In production, you might want to exit the application
        if (!app.Environment.IsDevelopment())
        {
            throw;
        }
    }
}

app.Logger.LogInformation("Excursion GPT API started successfully");
app.Logger.LogInformation("Environment: {Environment}", app.Environment.EnvironmentName);
app.Logger.LogInformation("Application Name: {AppName}", builder.Environment.ApplicationName);

app.Run();

// Make the implicit Program class public so tests can reference it
public partial class Program { }
