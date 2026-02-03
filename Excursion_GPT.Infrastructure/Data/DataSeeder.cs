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

        public DataSeeder(AppDbContext context, ILogger<DataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            _logger.LogInformation("Starting data seeding...");

            try
            {
                // Check if data already exists to avoid duplicates
                var hasUsers = await _context.Users.AnyAsync();
                var hasTracks = await _context.Tracks.AnyAsync();
                var hasBuildings = await _context.Buildings.AnyAsync();
                var hasModels = await _context.Models.AnyAsync();

                // Skip seeding only if tracks already exist (to avoid conflicts with migration data)
                if (hasTracks)
                {
                    _logger.LogInformation("Database already contains tracks. Skipping seeding to avoid conflicts.");
                    return;
                }

                // Ensure we have the users that the migration created
                await EnsureMigrationUsersExistAsync();
                await SeedTracksAsync();
                // Skip building seeding since migration already created some buildings
                await SeedModelsAsync();

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

        private async Task SeedBuildingsAsync()
        {
            // Skip building seeding since migration already created some buildings
            _logger.LogInformation("Skipping building seeding - migration already created buildings");
        }

        private async Task SeedModelsAsync()
        {
            if (await _context.Models.AnyAsync())
                return;

            // Use the building IDs that were created by the migration
            var models = new[]
            {
                new Model
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                    MinioObjectName = "skyscraper.glb",
                    Position = new List<double> { 0, 0, 0 },
                    Rotation = new List<double> { 0, 0, 0 },
                    Scale = 1.0,
                    BuildingId = Guid.Parse("10000000-0000-0000-0000-000000000001"), // Use existing building ID
                    TrackId = Guid.Parse("20000000-0000-0000-0000-000000000001")
                },
                new Model
                {
                    Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                    MinioObjectName = "temple.glb",
                    Position = new List<double> { 0, 0, 0 },
                    Rotation = new List<double> { 0, 0, 0 },
                    Scale = 1.0,
                    BuildingId = Guid.Parse("10000000-0000-0000-0000-000000000002"), // Use existing building ID
                    TrackId = Guid.Parse("20000000-0000-0000-0000-000000000002")
                }
            };

            await _context.Models.AddRangeAsync(models);
            _logger.LogInformation("Seeded {Count} models", models.Length);

            // Update buildings with model references
            var building1 = await _context.Buildings.FindAsync(Guid.Parse("10000000-0000-0000-0000-000000000001"));
            var building2 = await _context.Buildings.FindAsync(Guid.Parse("10000000-0000-0000-0000-000000000002"));

            if (building1 != null) building1.ModelId = Guid.Parse("30000000-0000-0000-0000-000000000001");
            if (building2 != null) building2.ModelId = Guid.Parse("30000000-0000-0000-0000-000000000002");
        }
    }
}
