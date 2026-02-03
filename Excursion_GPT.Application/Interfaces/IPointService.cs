using Excursion_GPT.Application.DTOs;

namespace Excursion_GPT.Application.Interfaces;

public interface IPointService
{
    /// <summary>
    /// Add a new point to track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="pointDto">Point creation request with name, type, position, and rotation</param>
    /// <returns>Created point information with ID</returns>
    Task<PointCreateResponseDto> AddPointToTrackAsync(string trackId, PointCreateRequestDto pointDto);

    /// <summary>
    /// Update point in track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="pointId">Point ID</param>
    /// <param name="pointDto">Point update request with optional name, type, position, and rotation</param>
    Task UpdatePointAsync(string trackId, string pointId, PointUpdateRequestDto pointDto);

    /// <summary>
    /// Delete point from track
    /// </summary>
    /// <param name="trackId">Track ID</param>
    /// <param name="pointId">Point ID</param>
    Task DeletePointAsync(string trackId, string pointId);
}
