using Excursion_GPT.Application.DTOs;

namespace Excursion_GPT.Application.Interfaces;

public interface ITrackService
{
    /// <summary>
    /// Get all tracks
    /// </summary>
    /// <returns>List of track items with ID and name</returns>
    Task<List<TrackListItemDto>> GetAllTracksAsync();

    /// <summary>
    /// Get track by ID with all points
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <returns>Track details with all points</returns>
    Task<TrackDetailsDto> GetTrackByIdAsync(string trackId);

    /// <summary>
    /// Create a new track
    /// </summary>
    /// <param name="trackDto">Track creation request with name</param>
    /// <returns>Created track information</returns>
    Task<TrackCreateResponseDto> CreateTrackAsync(TrackCreateRequestDto trackDto);

    /// <summary>
    /// Delete track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    Task DeleteTrackAsync(string trackId);
}
