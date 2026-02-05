using System.Text.Json;
using Excursion_GPT.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Application.Services
{
    public class BuildingDataService
    {
        private readonly ILogger<BuildingDataService> _logger;
        private List<BuildingData>? _cachedBuildings;
        private DateTime _lastLoadTime;
        private readonly object _lock = new object();
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);

        public BuildingDataService(ILogger<BuildingDataService> logger)
        {
            _logger = logger;
        }

        public List<BuildingData> GetBuildings()
        {
            lock (_lock)
            {
                // Return cached data if available and not expired
                if (_cachedBuildings != null && DateTime.UtcNow - _lastLoadTime < _cacheDuration)
                {
                    _logger.LogDebug("Returning cached building data ({Count} buildings)", _cachedBuildings.Count);
                    return _cachedBuildings;
                }

                // Load data from JSON file
                _cachedBuildings = LoadBuildingsFromJson();
                _lastLoadTime = DateTime.UtcNow;

                if (_cachedBuildings == null || _cachedBuildings.Count == 0)
                {
                    _logger.LogWarning("No building data loaded from JSON file");
                    return new List<BuildingData>();
                }

                _logger.LogInformation("Loaded {Count} buildings from JSON file", _cachedBuildings.Count);
                return _cachedBuildings;
            }
        }

        public BuildingData? GetBuildingById(string id)
        {
            var buildings = GetBuildings();
            return buildings.FirstOrDefault(b => b.OriginalId == id);
        }

        public BuildingData? GetBuildingByGuid(Guid guid)
        {
            var buildings = GetBuildings();
            return buildings.FirstOrDefault(b => b.DatabaseId == guid);
        }

        public List<BuildingData> GetBuildingsInArea(double centerX, double centerZ, double radius)
        {
            var buildings = GetBuildings();
            var result = new List<BuildingData>();

            foreach (var building in buildings)
            {
                // Calculate distance from center to building center
                var distance = CalculateDistance(centerX, centerZ, building.CenterX, building.CenterZ);
                if (distance <= radius)
                {
                    result.Add(building);
                }
            }

            _logger.LogDebug("Found {Count} buildings within {Radius}m of ({X}, {Z})",
                result.Count, radius, centerX, centerZ);

            return result;
        }

        private List<BuildingData> LoadBuildingsFromJson()
        {
            try
            {
                // Try multiple possible locations for the buildings.json file
                var possiblePaths = new[]
                {
                    // In the current directory (for development)
                    Path.Combine(Directory.GetCurrentDirectory(), "buildings.json"),
                    // One level up (common project structure)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "buildings.json"),
                    // Two levels up (from bin/Debug directory)
                    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "buildings.json"),
                    // In the app directory (for Docker containers)
                    Path.Combine(AppContext.BaseDirectory, "buildings.json"),
                    // In the app directory data folder
                    Path.Combine(AppContext.BaseDirectory, "data", "buildings.json"),
                    // Absolute path from environment variable
                    Environment.GetEnvironmentVariable("BUILDINGS_JSON_PATH") ?? string.Empty
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
                    _logger.LogError("buildings.json file not found in any of the expected locations");
                    return new List<BuildingData>();
                }

                // Read and parse JSON file
                var jsonData = File.ReadAllText(jsonFilePath);
                var jsonRoot = JsonSerializer.Deserialize<BuildingsJsonRoot>(jsonData);

                if (jsonRoot?.Buildings == null || jsonRoot.Buildings.Count == 0)
                {
                    _logger.LogWarning("No buildings found in JSON file");
                    return new List<BuildingData>();
                }

                // Convert JSON data to BuildingData objects
                var buildings = new List<BuildingData>();

                foreach (var buildingJson in jsonRoot.Buildings)
                {
                    if (buildingJson.Nodes == null || buildingJson.Nodes.Count == 0)
                    {
                        _logger.LogDebug("Skipping building {Id} with no nodes", buildingJson.Id);
                        continue;
                    }

                    // Calculate center point from polygon nodes
                    var center = CalculatePolygonCenter(buildingJson.Nodes);

                    // For Ekaterinburg data, X coordinates are negative but should be positive
                    // So we invert the sign of X for storage
                    double correctedX = -center.X;

                    // Generate deterministic GUID from building ID
                    var databaseId = GenerateDeterministicGuid(buildingJson.Id);

                    var buildingData = new BuildingData
                    {
                        OriginalId = buildingJson.Id,
                        DatabaseId = databaseId,
                        CenterX = correctedX,
                        CenterZ = center.Z,
                        Address = buildingJson.Address,
                        Height = buildingJson.Height,
                        Nodes = buildingJson.Nodes.Select(n => new BuildingNode
                        {
                            X = -n.X, // Invert X for Ekaterinburg data
                            Z = n.Z
                        }).ToList()
                    };

                    buildings.Add(buildingData);
                }

                _logger.LogInformation("Successfully loaded {Count} buildings from JSON file", buildings.Count);
                return buildings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load building data from JSON file");
                return new List<BuildingData>();
            }
        }

        private (double X, double Z) CalculatePolygonCenter(List<BuildingNodeJson> nodes)
        {
            // Calculate centroid using polygon area formula (works for any polygon)
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

        private double CalculateDistance(double x1, double z1, double x2, double z2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(z2 - z1, 2));
        }

        private Guid GenerateDeterministicGuid(string id)
        {
            // Generate a deterministic GUID from the building ID string
            // This ensures the same building always gets the same GUID
            using var md5 = System.Security.Cryptography.MD5.Create();
            byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(id));
            return new Guid(hash);
        }

        // Data classes for JSON deserialization
        private class BuildingsJsonRoot
        {
            public List<BuildingJson> Buildings { get; set; } = new List<BuildingJson>();
        }

        private class BuildingJson
        {
            public string Id { get; set; } = string.Empty;
            public List<BuildingNodeJson> Nodes { get; set; } = new List<BuildingNodeJson>();
            public string? Address { get; set; }
            public double Height { get; set; }
        }

        private class BuildingNodeJson
        {
            public double X { get; set; }
            public double Z { get; set; }
        }
    }

    public class BuildingData
    {
        public string OriginalId { get; set; } = string.Empty;
        public Guid DatabaseId { get; set; }
        public double CenterX { get; set; }
        public double CenterZ { get; set; }
        public string? Address { get; set; }
        public double Height { get; set; }
        public List<BuildingNode> Nodes { get; set; } = new List<BuildingNode>();
    }

    public class BuildingNode
    {
        public double X { get; set; }
        public double Z { get; set; }
    }
}
