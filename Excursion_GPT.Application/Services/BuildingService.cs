using Excursion_GPT.Application.Common;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Excursion_GPT.Application.Services;

public class BuildingService : IBuildingService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BuildingService> _logger;

    public BuildingService(AppDbContext context, ILogger<BuildingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<object>> GetBuildingsAroundPointAsync(BuildingsAroundPointRequestDto request)
    {
        _logger.LogInformation(
            "Getting buildings around point ({X}, {Z}) at distance {Distance}",
            request.Position.X, request.Position.Z, request.Distance);

        // Check if position is valid for Web Mercator coordinates
        if (!CoordinateConverter.IsValidWebMercator(request.Position.X, request.Position.Z))
        {
            throw new InvalidOperationException("Unknown terrain");
        }

        // Database stores coordinates in Web Mercator (meters), not degrees
        // So we can query directly in meters without conversion
        _logger.LogDebug("Querying buildings in Web Mercator coordinates (meters)");

        // For Ekaterinburg data, X coordinates in database are positive (inverted from JSON)
        // But API accepts both positive and negative coordinates
        // We need to handle both cases
        double queryX = request.Position.X;

        // If X is negative (as in API requests), invert it for database query
        // because BuildingDataSeeder stores positive X coordinates
        if (queryX < 0)
        {
            queryX = -queryX;
            _logger.LogDebug("Inverted X coordinate from {OriginalX} to {QueryX} for database query",
                request.Position.X, queryX);
        }

        // Query database for buildings within the specified distance in meters
        // Using simple bounding box in Web Mercator coordinates
        double minX = queryX - request.Distance;
        double maxX = queryX + request.Distance;
        double minZ = request.Position.Z - request.Distance;
        double maxZ = request.Position.Z + request.Distance;

        var buildingData = await _context.Buildings
            .Where(b => b.X >= minX && b.X <= maxX && b.Z >= minZ && b.Z <= maxZ)
            .Select(b => new
            {
                b.Id,
                b.X,
                b.Z,
                b.Address,
                b.Height,
                b.NodesJson,
                b.ModelId,
                b.Rotation,
                Distance = Math.Sqrt(Math.Pow(b.X - queryX, 2) + Math.Pow(b.Z - request.Position.Z, 2))
            })
            .OrderBy(b => b.Distance) // Order by distance to return closest buildings first
            .ToListAsync();

        _logger.LogInformation("Found {Count} buildings within bounding box (X: {MinX}-{MaxX}, Z: {MinZ}-{MaxZ})",
            buildingData.Count, minX, maxX, minZ, maxZ);

        // Create response
        var result = new List<object>();

        _logger.LogDebug("Processing {Count} buildings for response", buildingData.Count);

        foreach (var building in buildingData)
        {
            // Database stores coordinates in Web Mercator (meters), same as API response
            // So no conversion needed
            double responseX = building.X;

            // If original request had negative X, return negative X in response
            // to maintain consistency with API contract
            if (request.Position.X < 0)
            {
                responseX = -responseX;
            }

            // Parse NodesJson directly instead of using Nodes property
            List<List<double>>? nodes = null;
            if (!string.IsNullOrEmpty(building.NodesJson))
            {
                try
                {
                    nodes = JsonSerializer.Deserialize<List<List<double>>>(building.NodesJson);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize NodesJson for building {Id}", building.Id);
                }
            }

            // Check if building has a model
            if (building.ModelId.HasValue)
            {
                // Return building with model information
                var buildingResponse = new
                {
                    id = building.Id.ToString(),
                    model = building.ModelId.Value.ToString("N"),
                    x = responseX,
                    z = building.Z,
                    rot = building.Rotation ?? new List<double> { 0, 0, 0 },
                    address = building.Address,
                    height = building.Height
                };

                result.Add(buildingResponse);
                _logger.LogTrace("Added building {Id} with model {ModelId} to response (X: {X}, Z: {Z}, Rotation: {Rotation})",
                    building.Id, building.ModelId.Value, responseX, building.Z, building.Rotation);
            }
            else
            {
                // Return building with polygon nodes
                object buildingResponse;

                if (nodes != null && nodes.Count > 0)
                {
                    // Return building with actual polygon nodes
                    buildingResponse = new
                    {
                        id = building.Id.ToString(),
                        nodes = nodes.Select(node =>
                        {
                            double nodeX = node[0]; // X coordinate
                            double nodeZ = node[1]; // Z coordinate

                            // If original request had negative X, return negative X for nodes too
                            if (request.Position.X < 0)
                            {
                                nodeX = -nodeX;
                            }
                            return new { x = nodeX, z = nodeZ };
                        }).ToList(),
                        address = building.Address,
                        height = building.Height
                    };

                    _logger.LogTrace("Building {Id} has {NodeCount} actual polygon nodes",
                        building.Id, nodes.Count);
                }
                else
                {
                    // Fallback to simple bounding box if no nodes stored
                    buildingResponse = new
                    {
                        id = building.Id.ToString(),
                        nodes = new[]
                        {
                            new { x = responseX, z = building.Z },
                            new { x = responseX + 10.0, z = building.Z },
                            new { x = responseX + 10.0, z = building.Z + 10.0 },
                            new { x = responseX, z = building.Z + 10.0 }
                        },
                        address = building.Address,
                        height = building.Height
                    };

                    _logger.LogTrace("Building {Id} has no polygon nodes, using default 10x10 box",
                        building.Id);
                }

                result.Add(buildingResponse);
                _logger.LogTrace("Added building {Id} to response (X: {X}, Z: {Z}, Address: {Address}, Height: {Height})",
                    building.Id, responseX, building.Z, building.Address, building.Height);
            }
        }

        _logger.LogInformation("Created response with {Count} buildings", result.Count);

        // If no buildings found in database, return mock data for testing
        if (result.Count == 0)
        {
            _logger.LogWarning("No buildings found in database, returning mock data");

            // Use the original request coordinates for mock data
            double mockX = request.Position.X;
            double mockZ = request.Position.Z;

            // Add mock standard building (with nodes) in Web Mercator coordinates
            result.Add(new
            {
                id = "234234",
                nodes = new[]
                {
                    new { x = mockX, z = mockZ },
                    new { x = mockX + 10.0, z = mockZ },
                    new { x = mockX + 10.0, z = mockZ + 10.0 },
                    new { x = mockX, z = mockZ + 10.0 }
                },
                address = "Mock Address 1",
                height = 15.0
            });

            // Add mock building with model in Web Mercator coordinates
            result.Add(new
            {
                id = "234235",
                model = "model_001",
                x = mockX + 20.0,
                z = mockZ + 20.0,
                rot = new[] { 0.0, 1.5, 0.0 },
                address = "Mock Address 2",
                height = 25.0
            });
        }

        return result;
    }

    public async Task<BuildingByAddressResponseDto> GetBuildingByAddressAsync(BuildingByAddressRequestDto request)
    {
        _logger.LogInformation("Getting building by address: {Address}", request.Address);

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new InvalidOperationException("Address is required");
        }

        // In a real application, this would query a database or external service
        // For now, return mock data based on the requirements

        // Simulate not found scenario for certain addresses
        if (request.Address.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Building not found");
        }

        return await Task.FromResult(new BuildingByAddressResponseDto
        {
            Address = request.Address,
            Nodes = new List<PositionDto>
            {
                new PositionDto { X = 66.3333, Z = 65.4444 },
                new PositionDto { X = 66.3334, Z = 65.4445 },
                new PositionDto { X = 66.3335, Z = 65.4446 }
            },
            Height = 25.5,
            Position = new PositionDto { X = 66.3334, Z = 65.4445 },
            ModelUrl = request.Address.Contains("model")
                ? "https://storage.example.com/models/model_001.glb"
                : null
        });
    }
}
