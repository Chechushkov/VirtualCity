using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Application.Services;

public class PointService : IPointService
{
    private readonly ILogger<PointService> _logger;

    public PointService(ILogger<PointService> logger)
    {
        _logger = logger;
    }

    public async Task<PointCreateResponseDto> AddPointToTrackAsync(string trackId, PointCreateRequestDto pointDto)
    {
        _logger.LogInformation("Adding point to track ID: {TrackId}", trackId);

        // Validate input
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new InvalidOperationException("Track ID is required");
        }

        if (string.IsNullOrWhiteSpace(pointDto.Name))
        {
            throw new InvalidOperationException("Point name is required");
        }

        if (string.IsNullOrWhiteSpace(pointDto.Type))
        {
            throw new InvalidOperationException("Point type is required");
        }

        if (pointDto.Position == null || pointDto.Position.Count != 3)
        {
            throw new InvalidOperationException("Position must be an array of 3 numbers [x, y, z]");
        }

        if (pointDto.Rotation == null || pointDto.Rotation.Count != 3)
        {
            throw new InvalidOperationException("Rotation must be an array of 3 numbers [a, b, c]");
        }

        // Check if track exists (simulate not found for certain IDs)
        if (trackId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Track not found");
        }

        try
        {
            // In a real application, this would:
            // 1. Validate the track exists
            // 2. Create a new point entity
            // 3. Set the track ID and other properties
            // 4. Save to database
            // 5. Return the created point ID

            // Generate a unique point ID
            var pointId = $"point_{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            _logger.LogInformation("Point created successfully: {PointId} for track: {TrackId}", pointId, trackId);

            return await Task.FromResult(new PointCreateResponseDto
            {
                Id = pointId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add point to track: {TrackId}", trackId);
            throw new InvalidOperationException("Failed to add point to track");
        }
    }

    public async Task UpdatePointAsync(string trackId, string pointId, PointUpdateRequestDto pointDto)
    {
        _logger.LogInformation("Updating point {PointId} in track {TrackId}", pointId, trackId);

        // Validate input
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new InvalidOperationException("Track ID is required");
        }

        if (string.IsNullOrWhiteSpace(pointId))
        {
            throw new InvalidOperationException("Point ID is required");
        }

        // Check if track exists (simulate not found for certain IDs)
        if (trackId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Track not found");
        }

        // Check if point exists (simulate not found for certain IDs)
        if (pointId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Point not found");
        }

        // Validate position if provided
        if (pointDto.Position != null && pointDto.Position.Count != 3)
        {
            throw new InvalidOperationException("Position must be an array of 3 numbers [x, y, z]");
        }

        // Validate rotation if provided
        if (pointDto.Rotation != null && pointDto.Rotation.Count != 3)
        {
            throw new InvalidOperationException("Rotation must be an array of 3 numbers [a, b, c]");
        }

        try
        {
            // In a real application, this would:
            // 1. Validate the track exists
            // 2. Validate the point exists and belongs to the track
            // 3. Update the point properties (only non-null values)
            // 4. Save changes to database

            // Simulate update delay
            await Task.Delay(100);

            _logger.LogInformation("Point updated successfully: {PointId} in track: {TrackId}", pointId, trackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update point {PointId} in track {TrackId}", pointId, trackId);
            throw new InvalidOperationException("Failed to update point");
        }
    }

    public async Task DeletePointAsync(string trackId, string pointId)
    {
        _logger.LogInformation("Deleting point {PointId} from track {TrackId}", pointId, trackId);

        // Validate input
        if (string.IsNullOrWhiteSpace(trackId))
        {
            throw new InvalidOperationException("Track ID is required");
        }

        if (string.IsNullOrWhiteSpace(pointId))
        {
            throw new InvalidOperationException("Point ID is required");
        }

        // Check if track exists (simulate not found for certain IDs)
        if (trackId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Track not found");
        }

        // Check if point exists (simulate not found for certain IDs)
        if (pointId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Point not found");
        }

        try
        {
            // In a real application, this would:
            // 1. Validate the track exists
            // 2. Validate the point exists and belongs to the track
            // 3. Delete the point from database
            // 4. Handle any cascading deletions if needed

            // Simulate deletion delay
            await Task.Delay(100);

            _logger.LogInformation("Point deleted successfully: {PointId} from track: {TrackId}", pointId, trackId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete point {PointId} from track {TrackId}", pointId, trackId);
            throw new InvalidOperationException("Failed to delete point");
        }
    }
}
