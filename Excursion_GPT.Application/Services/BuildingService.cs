using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        // Check if position is at north pole (example of "Unknown terrain")
        if (Math.Abs(request.Position.X) > 85 || Math.Abs(request.Position.Z) > 85)
        {
            throw new InvalidOperationException("Unknown terrain");
        }

        // In a real application, this would involve spatial queries
        // For now, we'll return mock data based on the requirements

        var result = new List<object>();

        // Add some standard buildings (with nodes)
        result.Add(new
        {
            id = "234234",
            nd = new[]
            {
                new { lat = 66.3333, lng = 65.4444 },
                new { lat = 66.3334, lng = 65.4445 },
                new { lat = 66.3335, lng = 65.4446 }
            }
        });

        result.Add(new
        {
            id = "234235",
            nd = new[]
            {
                new { lat = 66.4333, lng = 65.5444 },
                new { lat = 66.4334, lng = 65.5445 }
            }
        });

        // Add some buildings with models
        result.Add(new
        {
            id = "234236",
            model = "model_001",
            lat = 67.3333,
            lng = 68.4444,
            rot = new[] { 0.0, 1.5, 0.0 }
        });

        result.Add(new
        {
            id = "234237",
            model = "model_002",
            lat = 67.4333,
            lng = 68.5444,
            rot = new[] { 0.0, 0.5, 0.0 }
        });

        return await Task.FromResult(result);
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
