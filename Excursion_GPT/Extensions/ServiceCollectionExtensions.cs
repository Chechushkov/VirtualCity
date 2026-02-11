using System.Text;
using Excursion_GPT.Application.Common;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Application.Services;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Minio;
using Excursion_GPT.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using AutoMapper;
using Npgsql;

namespace Excursion_GPT.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
        {
            var dataSourceBuilder = new NpgsqlDataSourceBuilder(configuration.GetConnectionString("DefaultConnection"));
            dataSourceBuilder.EnableDynamicJson();
            var dataSource = dataSourceBuilder.Build();

            options.UseNpgsql(dataSource, npgsqlOptions =>
                npgsqlOptions.MigrationsAssembly("Excursion_GPT.Infrastructure"));
        });

        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile).Assembly);

        // MinIO
        services.AddSingleton<IMinioClient>(sp =>
        {
            var minioConfig = configuration.GetSection("Minio");
            return new MinioClient()
                .WithEndpoint(minioConfig["Endpoint"])
                .WithCredentials(minioConfig["AccessKey"], minioConfig["SecretKey"])
                .Build();
        });
        services.AddScoped<IMinioService, MinioService>();

        // Security
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // Application Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IBuildingService, BuildingService>();
        services.AddScoped<IModelService, ModelService>();
        services.AddScoped<ITrackService, TrackService>();
        services.AddScoped<IPointService, PointService>();

        // Data Seeders
        services.AddScoped<BuildingDataSeeder>();
        services.AddScoped<DataSeeder>();

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? throw new ArgumentNullException("JWT Key not configured."));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false; // Set to true in production
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No tolerance for expiration date
            };

            // Handle token validation events for better error handling
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
            options.AddPolicy("CreatorPolicy", policy => policy.RequireRole("Admin", "Creator"));
            options.AddPolicy("UserPolicy", policy => policy.RequireRole("Admin", "Creator", "User"));
        });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Excursion GPT API",
                Version = "v1",
                Description = "API for managing excursions and building models",
                Contact = new OpenApiContact
                {
                    Name = "Excursion GPT Team",
                    Email = "support@excursiongpt.com"
                }
            });

            // Configure JWT bearer token authentication in Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1Ni...\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
                BearerFormat = "JWT"
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

            // Enable XML comments for better documentation
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }

            // Configure file upload support for Swagger
            // Map IFormFile to Swagger file type
            c.MapType<IFormFile>(() => new OpenApiSchema
            {
                Type = "string",
                Format = "binary"
            });
        });

        return services;
    }
}
