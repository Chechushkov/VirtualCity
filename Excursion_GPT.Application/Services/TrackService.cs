using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Application.Services;

public class TrackService : ITrackService
{
    private readonly AppDbContext _context;
    private readonly ILogger<TrackService> _logger;

    public TrackService(AppDbContext context, ILogger<TrackService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<TrackListItemDto>> GetAllTracksAsync()
    {
        _logger.LogInformation("Getting all tracks");

        try
        {
            // In a real application, this would query the database
            // For now, return mock data based on the requirements

            var tracks = new List<TrackListItemDto>
            {
                new TrackListItemDto
                {
                    Id = "track_001",
                    Name = "City Center Tour"
                },
                new TrackListItemDto
                {
                    Id = "track_002",
                    Name = "Historical Buildings"
                },
                new TrackListItemDto
                {
                    Id = "track_003",
                    Name = "Modern Architecture"
                },
                new TrackListItemDto
                {
                    Id = "track_004",
                    Name = "Parks and Gardens"
                }
            };

            return await Task.FromResult(tracks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all tracks");
            throw new InvalidOperationException("Failed to retrieve tracks");
        }
    }

    public async Task<TrackDetailsDto> GetTrackByIdAsync(string trackId)
    {
        _logger.LogInformation("Getting track by ID: {TrackId}", trackId);

        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new InvalidOperationException("Track ID is required");
        }

        // Check if track exists (simulate not found for certain IDs)
        if (trackId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Track not found");
        }

        try
        {
            // In a real application, this would query the database with points
            // For now, return mock data based on the requirements

            var track = new TrackDetailsDto
            {
                Id = trackId,
                Name = GetTrackNameById(trackId),
                Points = GetMockPointsForTrack(trackId)
            };

            return await Task.FromResult(track);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get track by ID: {TrackId}", trackId);
            throw new InvalidOperationException("Failed to retrieve track");
        }
    }

    public async Task<TrackCreateResponseDto> CreateTrackAsync(TrackCreateRequestDto trackDto)
    {
        _logger.LogInformation("Creating new track: {TrackName}", trackDto.Name);

        if (string.IsNullOrWhiteSpace(trackDto.Name))
        {
            throw new InvalidOperationException("Track name is required");
        }

        try
        {
            // In a real application, this would:
            // 1. Validate the request
            // 2. Create a new track entity
            // 3. Save to database
            // 4. Return the created track

            // Generate a unique track ID
            var trackId = $"track_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            _logger.LogInformation("Track created successfully: {TrackId}", trackId);

            return await Task.FromResult(new TrackCreateResponseDto
            {
                Id = trackId,
                Name = trackDto.Name
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create track: {TrackName}", trackDto.Name);
            throw new InvalidOperationException("Failed to create track");
        }
    }

    public async Task DeleteTrackAsync(string trackId)
    {
        _logger.LogInformation("Deleting track with ID: {TrackId}", trackId);

        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new InvalidOperationException("Track ID is required");
        }

        // Check if track exists (simulate not found for certain IDs)
        if (trackId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Track not found");
        }

        try
        {
            // In a real application, this would:
            // 1. Check if track exists
            // 2. Delete all related points
            // 3. Delete any related models (as per requirements)
            // 4. Delete the track from database

            // Simulate deletion delay
            await Task.Delay(100);

            _logger.LogInformation("Track deleted successfully: {TrackId}", trackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete track: {TrackId}", trackId);
            throw new InvalidOperationException("Failed to delete track");
        }
    }

    private string GetTrackNameById(string trackId)
    {
        return trackId switch
        {
            "track_001" => "City Center Tour",
            "track_002" => "Historical Buildings",
            "track_003" => "Modern Architecture",
            "track_004" => "Parks and Gardens",
            _ => $"Track {trackId}"
        };
    }

    private List<PointDto> GetMockPointsForTrack(string trackId)
    {
        var points = new List<PointDto>();

        // Add different points based on track ID
        switch (trackId)
        {
            case "track_001":
                points.AddRange(new[]
                {
                    new PointDto
                    {
                        Id = "point_001_1",
                        Name = "Main Square",
                        Lat = 55.7558,
                        Lng = 37.6173,
                        Type = "viewpoint",
                        Position = new List<double> { 55.7558, 0.0, 37.6173 },
                        Rotation = new List<double> { 0.0, 0.0, 0.0 },
                        RotationRestricted = false,
                        TiltRestricted = false,
                        MovementRestricted = false
                    },
                    new PointDto
                    {
                        Id = "point_001_2",
                        Name = "City Hall",
                        Lat = 55.7560,
                        Lng = 37.6175,
                        Type = "info",
                        Position = new List<double> { 55.7560, 0.0, 37.6175 },
                        Rotation = new List<double> { 0.0, 1.57, 0.0 },
                        RotationRestricted = true,
                        TiltRestricted = true,
                        MovementRestricted = true
                    }
                });
                break;

            case "track_002":
                points.AddRange(new[]
                {
                    new PointDto
                    {
                        Id = "point_002_1",
                        Name = "Old Cathedral",
                        Lat = 55.7444,
                        Lng = 37.6185,
                        Type = "historical",
                        Position = new List<double> { 55.7444, 0.0, 37.6185 },
                        Rotation = new List<double> { 0.0, 0.78, 0.0 },
                        RotationRestricted = false,
                        TiltRestricted = true,
                        MovementRestricted = false
                    }
                });
                break;

            default:
                // Default points for other tracks
                points.Add(new PointDto
                {
                    Id = $"point_{trackId}_1",
                    Name = "Starting Point",
                    Lat = 55.7512,
                    Lng = 37.6184,
                    Type = "default",
                    Position = new List<double> { 55.7512, 0.0, 37.6184 },
                    Rotation = new List<double> { 0.0, 0.0, 0.0 },
                    RotationRestricted = false,
                    TiltRestricted = false,
                    MovementRestricted = false
                });
                break;
        }

        return points;
    }
}
