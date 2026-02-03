using System.Security.Claims;
using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Excursion_GPT.Controllers;

[ApiController]
[Route("tracks")]
public class TracksController : ControllerBase
{
    private readonly ITrackService _trackService;
    private readonly IPointService _pointService;
    private readonly ILogger<TracksController> _logger;

    public TracksController(ITrackService trackService, IPointService pointService, ILogger<TracksController> logger)
    {
        _trackService = trackService;
        _pointService = pointService;
        _logger = logger;
    }

    /// <summary>
    /// Get all tracks
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<TrackListItemDto>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    public async Task<ActionResult<List<TrackListItemDto>>> GetAllTracks()
    {
        _logger.LogInformation("Getting all tracks");

        try
        {
            var tracks = await _trackService.GetAllTracksAsync();
            return Ok(tracks);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
    }

    /// <summary>
    /// Get track by ID with all points
    /// </summary>
    [HttpGet("{trackId}")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrackDetailsDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackNotFoundErrorDto))]
    public async Task<ActionResult<TrackDetailsDto>> GetTrackById(string trackId)
    {
        _logger.LogInformation("Getting track by ID: {TrackId}", trackId);

        try
        {
            var track = await _trackService.GetTrackByIdAsync(trackId);
            return Ok(track);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new TrackNotFoundErrorDto());
        }
    }

    /// <summary>
    /// Create a new track
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(TrackCreateResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    public async Task<ActionResult<TrackCreateResponseDto>> CreateTrack([FromBody] TrackCreateRequestDto trackDto)
    {
        _logger.LogInformation("Creating new track: {TrackName}", trackDto.Name);

        try
        {
            var createdTrack = await _trackService.CreateTrackAsync(trackDto);
            return CreatedAtAction(nameof(GetTrackById), new { trackId = createdTrack.Id }, createdTrack);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
    }

    /// <summary>
    /// Delete track
    /// </summary>
    [HttpDelete("{trackId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackNotFoundErrorDto))]
    public async Task<ActionResult> DeleteTrack(string trackId)
    {
        _logger.LogInformation("Deleting track with ID: {TrackId}", trackId);

        try
        {
            await _trackService.DeleteTrackAsync(trackId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new TrackNotFoundErrorDto());
        }
    }

    /// <summary>
    /// Add a new point to track
    /// </summary>
    [HttpPost("{trackId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PointCreateResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackNotFoundErrorDto))]
    public async Task<ActionResult<PointCreateResponseDto>> AddPointToTrack(
        string trackId,
        [FromBody] PointCreateRequestDto pointDto)
    {
        _logger.LogInformation("Adding point to track ID: {TrackId}", trackId);

        try
        {
            var createdPoint = await _pointService.AddPointToTrackAsync(trackId, pointDto);
            return CreatedAtAction(nameof(GetTrackById), new { trackId = trackId }, createdPoint);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackNotFoundErrorDto());
        }
    }

    /// <summary>
    /// Update point in track
    /// </summary>
    [HttpPut("{trackId}/{pointId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackNotFoundErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(PointErrorDto))]
    public async Task<ActionResult> UpdatePoint(
        string trackId,
        string pointId,
        [FromBody] PointUpdateRequestDto pointDto)
    {
        _logger.LogInformation("Updating point {PointId} in track {TrackId}", pointId, trackId);

        try
        {
            await _pointService.UpdatePointAsync(trackId, pointId, pointDto);
            return Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackNotFoundErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("point"))
        {
            return NotFound(new PointErrorDto());
        }
    }

    /// <summary>
    /// Delete point from track
    /// </summary>
    [HttpDelete("{trackId}/{pointId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackNotFoundErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(PointErrorDto))]
    public async Task<ActionResult> DeletePoint(string trackId, string pointId)
    {
        _logger.LogInformation("Deleting point {PointId} from track {TrackId}", pointId, trackId);

        try
        {
            await _pointService.DeletePointAsync(trackId, pointId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackNotFoundErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("point"))
        {
            return NotFound(new PointErrorDto());
        }
    }
}
