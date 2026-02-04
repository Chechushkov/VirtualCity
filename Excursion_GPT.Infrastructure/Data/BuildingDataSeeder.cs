using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Excursion_GPT.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Infrastructure.Data
{
    public class BuildingDataSeeder
    {
        private readonly AppDbContext _context;
        private readonly ILogger<BuildingDataSeeder> _logger;

        public BuildingDataSeeder(AppDbContext context, ILogger<BuildingDataSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedFromJsonAsync(string jsonFilePath)
        {
            _logger.LogInformation("Starting building data seeding from JSON file: {FilePath}", jsonFilePath);

            try
            {
                // Check if we already have a significant number of buildings
                var buildingCount = await _context.Buildings.CountAsync();
                if (buildingCount > 1000)
                {
                    _logger.LogInformation("Database already contains {Count} buildings. Skipping seeding.", buildingCount);
                    return;
                }

                // Read and parse JSON file
                _logger.LogInformation("Reading JSON file...");
                var jsonData = await File.ReadAllTextAsync(jsonFilePath);
                var buildingsData = JsonSerializer.Deserialize<BuildingsJsonData>(jsonData);

                if (buildingsData?.Buildings == null || buildingsData.Buildings.Count == 0)
                {
                    _logger.LogWarning("No buildings found in JSON file.");
                    return;
                }

                _logger.LogInformation("Found {Count} buildings in JSON file", buildingsData.Buildings.Count);

                // Process buildings in batches to avoid memory issues
                var batchSize = 1000;
                var processedCount = 0;
                var savedCount = 0;

                for (int i = 0; i < buildingsData.Buildings.Count; i += batchSize)
                {
                    var batch = buildingsData.Buildings.Skip(i).Take(batchSize).ToList();
                    _logger.LogInformation("Processing batch {BatchNumber} ({Start}-{End})",
                        i / batchSize + 1, i, Math.Min(i + batchSize, buildingsData.Buildings.Count) - 1);

                    var entities = new List<Building>();

                    foreach (var buildingData in batch)
                    {
                        try
                        {
                            var building = ConvertToBuildingEntity(buildingData);
                            if (building != null)
                            {
                                entities.Add(building);
                                processedCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to convert building with ID {BuildingId}", buildingData.Id);
                        }
                    }

                    // Save batch to database
                    if (entities.Count > 0)
                    {
                        await _context.Buildings.AddRangeAsync(entities);
                        await _context.SaveChangesAsync();
                        savedCount += entities.Count;
                        _logger.LogInformation("Saved {Count} buildings to database (total: {Total})", entities.Count, savedCount);

                        // Clear tracking to free memory
                        _context.ChangeTracker.Clear();
                    }
                }

                _logger.LogInformation("Building data seeding completed. Processed: {Processed}, Saved: {Saved}",
                    processedCount, savedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding building data from JSON");
                throw;
            }
        }

        private Building? ConvertToBuildingEntity(BuildingJsonData buildingData)
        {
            if (buildingData.Nodes == null || buildingData.Nodes.Count == 0)
            {
                _logger.LogWarning("Building {Id} has no nodes, skipping", buildingData.Id);
                return null;
            }

            // Calculate center point from polygon nodes
            var center = CalculatePolygonCenter(buildingData.Nodes);

            // Convert Web Mercator coordinates to latitude/longitude
            var (latitude, longitude) = WebMercatorToLatLon(center.X, center.Z);

            // Log conversion for debugging
            _logger.LogDebug("Converted building {Id}: X={X}, Z={Z} -> Lat={Lat}, Lon={Lon}",
                buildingData.Id, center.X, center.Z, latitude, longitude);

            // Create building entity
            var building = new Building
            {
                Id = GenerateDeterministicGuid(buildingData.Id),
                Latitude = latitude,
                Longitude = longitude,
                ModelId = null, // No models initially
                Rotation = null // No rotation for standard buildings
            };

            return building;
        }

        private (double X, double Z) CalculatePolygonCenter(List<NodeJsonData> nodes)
        {
            // Calculate centroid using polygon area formula (works for any polygon)
            // This is more accurate than simple average for irregular polygons

            if (nodes.Count < 3)
            {
                // For less than 3 points, use simple average
                double sumX = 0;
                double sumZ = 0;
                foreach (var node in nodes)
                {
                    sumX += node.X;
                    sumZ += node.Z;
                }
                return (sumX / nodes.Count, sumZ / nodes.Count);
            }

            double area = 0;
            double centroidX = 0;
            double centroidZ = 0;

            for (int i = 0; i < nodes.Count; i++)
            {
                int j = (i + 1) % nodes.Count;
                double xi = nodes[i].X;
                double zi = nodes[i].Z;
                double xj = nodes[j].X;
                double zj = nodes[j].Z;

                double cross = (xi * zj - xj * zi);
                area += cross;
                centroidX += (xi + xj) * cross;
                centroidZ += (zi + zj) * cross;
            }

            area /= 2.0;
            double factor = 1.0 / (6.0 * area);

            centroidX *= factor;
            centroidZ *= factor;

            return (centroidX, centroidZ);
        }

        private (double Latitude, double Longitude) WebMercatorToLatLon(double x, double z)
        {
            // Web Mercator (EPSG:3857) to WGS84 (EPSG:4326) conversion
            // Coordinates are in meters from the origin (0,0 at 180W, 85.06N)
            // For Ekaterinburg data, x coordinates are negative but should be positive
            // So we invert the sign of x

            const double earthRadius = 6378137.0; // meters
            const double originShift = Math.PI * earthRadius; // 20037508.342789244

            // Invert x sign for Ekaterinburg data (east longitude should be positive)
            double correctedX = -x;

            // Normalize coordinates
            double lon = (correctedX / originShift) * 180.0;
            double lat = (z / originShift) * 180.0;

            // Convert to latitude
            lat = 180.0 / Math.PI * (2.0 * Math.Atan(Math.Exp(lat * Math.PI / 180.0)) - Math.PI / 2.0);

            return (lat, lon);
        }

        private Guid GenerateDeterministicGuid(string id)
        {
            // Generate a deterministic GUID from the building ID string
            // This ensures the same building always gets the same GUID
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(id));
            return new Guid(hash);
        }

        // JSON data classes
        private class BuildingsJsonData
        {
            [JsonPropertyName("buildings")]
            public List<BuildingJsonData> Buildings { get; set; } = new List<BuildingJsonData>();
        }

        private class BuildingJsonData
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("nodes")]
            public List<NodeJsonData> Nodes { get; set; } = new List<NodeJsonData>();

            [JsonPropertyName("address")]
            public string? Address { get; set; }

            [JsonPropertyName("height")]
            public double Height { get; set; }

            // For debugging/logging
            public override string ToString()
            {
                return $"Building {Id}: {Nodes?.Count ?? 0} nodes, Height: {Height}, Address: {Address ?? "null"}";
            }
        }

        private class NodeJsonData
        {
            [JsonPropertyName("x")]
            public double X { get; set; }

            [JsonPropertyName("z")]
            public double Z { get; set; }
        }
    }
}
