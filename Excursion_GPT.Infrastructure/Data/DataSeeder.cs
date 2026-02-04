using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Excursion_GPT.Domain.Entities;
using Excursion_GPT.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Infrastructure.Data
{
    public class DataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DataSeeder> _logger;
        private readonly BuildingDataSeeder _buildingDataSeeder;

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger, BuildingDataSeeder buildingDataSeeder)
        {
            _context = context;
            _logger = logger;
            _buildingDataSeeder = buildingDataSeeder;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Starting data seeding...");

            try
            {
                // Check if data already exists to avoid duplicates
                var hasUsers = await _context.Users.AnyAsync();
                var hasTracks = await _context.Tracks.AnyAsync();
                var buildingCount = await _context.Buildings.CountAsync();
                var hasModels = await _context.Models.AnyAsync();

                // Skip seeding only if tracks already exist (to avoid conflicts with migration data)
                if (hasTracks && buildingCount > 1000)
                {
                    _logger.LogInformation("Database already contains tracks and {Count} buildings. Skipping seeding to avoid conflicts.", buildingCount);
                    return;
                }

                // Ensure we have the users that the migration created
                await EnsureMigrationUsersExistAsync();
                await SeedTracksAsync();

                // Seed buildings from JSON file if we have less than 1000 buildings
                if (buildingCount < 1000)
                {
                    // Clear existing buildings if they're just the default ones
                    if (buildingCount > 0 && buildingCount <= 2)
                    {
                        _logger.LogInformation("Clearing {Count} default buildings before loading from JSON...", buildingCount);
                        var defaultBuildings = await _context.Buildings.ToListAsync();
                        _context.Buildings.RemoveRange(defaultBuildings);
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Default buildings cleared.");
                    }

                    await SeedBuildingsFromJsonAsync();
                }
                else
                {
                    _logger.LogInformation("Database already contains {Count} buildings. Skipping building seeding.", buildingCount);
                }

                // Only seed models if we have buildings
                var buildingCountAfterSeeding = await _context.Buildings.CountAsync();
                if (buildingCountAfterSeeding > 0)
                {
                    await SeedModelsAsync();
                }
                else
                {
                    _logger.LogWarning("No buildings available to seed models. Skipping model seeding.");
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Data seeding completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding data.");
                throw;
            }
        }

        private async Task EnsureMigrationUsersExistAsync()
        {
            // Check if the migration users exist, and create them if they don't
            var adminUser = await _context.Users.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000001"));
            var creatorUser = await _context.Users.FindAsync(Guid.Parse("00000000-0000-0000-0000-000000000002"));

            if (adminUser == null)
            {
                adminUser = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = "Admin User",
                    Login = "admin",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("adminpass"),
                    Phone = "+1234567890",
                    SchoolName = "Admin Academy",
                    Role = Role.Admin
                };
                await _context.Users.AddAsync(adminUser);
                _logger.LogInformation("Created admin user");
            }

            if (creatorUser == null)
            {
                creatorUser = new User
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = "Creator User",
                    Login = "creator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("creatorpass"),
                    Phone = "+0987654321",
                    SchoolName = "Art School",
                    Role = Role.Creator
                };
                await _context.Users.AddAsync(creatorUser);
                _logger.LogInformation("Created creator user");
            }

            // Save changes immediately so tracks can reference these users
            await _context.SaveChangesAsync();
        }

        private async Task SeedTracksAsync()
        {
            if (await _context.Tracks.AnyAsync())
                return;

            var tracks = new[]
            {
                new Track
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
                    Name = "Moscow City Tour",
                    CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000001") // Admin user
                },
                new Track
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
                    Name = "Historical Landmarks",
                    CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000002") // Creator user
                },
                new Track
                {
                    Id = Guid.Parse("20000000-0000-0000-0000-000000000003"),
                    Name = "Test Track",
                    CreatorId = Guid.Parse("00000000-0000-0000-0000-000000000002") // Creator user (since test user doesn't exist in migration)
                }
            };

            await _context.Tracks.AddRangeAsync(tracks);
            _logger.LogInformation("Seeded {Count} tracks", tracks.Length);
        }

        private async Task SeedBuildingsFromJsonAsync()
        {
            _logger.LogInformation("Starting building data seeding from JSON file...");

            try
            {
                // Try multiple possible locations for the buildings.json file
                var possiblePaths = new[]
                {
                    // 1. In the current directory (for development)
                    Path.Combine(Directory.GetCurrentDirectory(), "buildings.json"),
                    // 2. One level up (common project structure)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "buildings.json"),
                    // 3. Two levels up (from bin/Debug directory)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "buildings.json"),
                    // 4. In the project root (when running from solution root)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "buildings.json"),
                    // 5. Absolute path from environment variable (for Docker)
                    Environment.GetEnvironmentVariable("BUILDINGS_JSON_PATH") ?? string.Empty,
                    // 6. In the app directory (for Docker containers)
                    Path.Combine(AppContext.BaseDirectory, "buildings.json"),
                    // 7. In the app directory data folder
                    Path.Combine(AppContext.BaseDirectory, "data", "buildings.json")
                };

                string? jsonFilePath = null;

                foreach (var path in possiblePaths)
                {
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    {
                        jsonFilePath = path;
                        _logger.LogInformation("Found buildings.json file at: {Path}", path);
                        break;
                    }
                }

                if (jsonFilePath == null)
                {
                    _logger.LogWarning("buildings.json file not found in any of the expected locations. Using default building data.");
                    await SeedDefaultBuildingsAsync();
                    return;
                }

                _logger.LogInformation("Using buildings.json file at: {Path}", jsonFilePath);
                await _buildingDataSeeder.SeedFromJsonAsync(jsonFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to seed buildings from JSON. Using default building data.");
                await SeedDefaultBuildingsAsync();
            }
        }

        private async Task SeedDefaultBuildingsAsync()
        {
            if (await _context.Buildings.AnyAsync())
                return;

            var buildings = new[]
            {
                new Building
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
                    Latitude = 55.751244,
                    Longitude = 37.618423,
                    ModelId = null,
                    Rotation = null
                },
                new Building
                {
                    Id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
                    Latitude = 55.755826,
                    Longitude = 37.6173,
                    ModelId = null,
                    Rotation = null
                }
            };

            await _context.Buildings.AddRangeAsync(buildings);
            _logger.LogInformation("Seeded {Count} default buildings", buildings.Length);
        }

        private async Task SeedModelsAsync()
        {
            if (await _context.Models.AnyAsync())
                return;

            // Get some buildings to attach models to
            var buildings = await _context.Buildings
                .OrderBy(b => b.Id)
                .Take(2)
                .ToListAsync();

            if (buildings.Count < 2)
            {
                _logger.LogWarning("Not enough buildings to seed models. Need at least 2, found {Count}.", buildings.Count);
                return;
            }

            var models = new[]
            {
                new Model
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    MinioObjectName = "skyscraper.glb",
                    Position = new List<double> { 0, 0, 0 },
                    Rotation = new List<double> { 0, 0, 0 },
                    Scale = 1.0,
                    BuildingId = buildings[0].Id,
                    TrackId = Guid.Parse("20000000-0000-0000-0000-000000000001")
                },
                new Model
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    MinioObjectName = "temple.glb",
                    Position = new List<double> { 0, 0, 0 },
                    Rotation = new List<double> { 0, 0, 0 },
                    Scale = 1.0,
                    BuildingId = buildings[1].Id,
                    TrackId = Guid.Parse("20000000-0000-0000-0000-000000000002")
                }
            };

            await _context.Models.AddRangeAsync(models);
            _logger.LogInformation("Seeded {Count} models", models.Length);

            // Update buildings with model references
            buildings[0].ModelId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            buildings[1].ModelId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        }
    }
}
