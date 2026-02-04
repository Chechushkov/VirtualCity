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
    /// Получить список экскурсий
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// [
    ///   { "id": "track_id", "name": "track_name" },
    ///   ...
    /// ]
    /// ```
    ///
    /// **Ошибки:**
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
    /// Получить экскурсию со всеми точками, которые в ней
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// [
    ///   { "id": "point_id", "name": "point_name", "lat": 66.3333, "lng": 55.3333, ... },
    ///   ...
    /// ]
    /// ```
    /// Для каждой точки точно будет нужно какое-то название, координаты, и какая-нибудь дополнительная информация, которая добавится позже, например, ограничения на поведение в точке (запрет поворота, запрет наклона и т.п.)
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если экскурсия не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
    /// Создать новую экскурсию
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "name": "track_name"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "track_id",
    ///   "name": "track_name"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **403 Forbidden**: Если по роли не положено создавать экскурсии
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
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
    /// Удалить экскурсию
    /// </summary>
    /// <remarks>
    /// При удалении экскурсии модели, привязанные к экскурсии должны быть тоже почищены, по идее.
    ///
    /// **Ошибки:**
    /// - **403 Forbidden**: Если по роли не положено удалять экскурсии
    /// - **404 Not Found**: Если указанная экскурсия уже была удалена, например
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
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
    /// Добавить новую точку в экскурсию
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "name": "point_name",
    ///   "type": "point_type",
    ///   "position": [x, y, z],
    ///   "rotation": [a, b, c]
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "point_id"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **403 Forbidden**: Если по роли не положено создавать точки в экскурсии
    /// - **404 Not Found**: Если указанная экскурсия уже была удалена, например
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
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
    /// Переименовать или изменить настройки точки
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "name": "point_name",
    ///   "type": "point_type",
    ///   "position": [x, y, z],
    ///   "rotation": [a, b, c]
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **403 Forbidden**: Если по роли не положено изменять точки в экскурсии
    /// - **404 Not Found**: Если указанная экскурсия уже была удалена, например
    /// - **404 Not Found**: Если указанная точка уже была удалена, например
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
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
    /// Удалить точку из экскурсии
    /// </summary>
    /// <remarks>
    /// **Ошибки:**
    /// - **403 Forbidden**: Если по роли не положено удалять точки в экскурсии
    /// - **404 Not Found**: Если указанная экскурсия уже была удалена, например
    /// - **404 Not Found**: Если указанная точка уже была удалена, например
    /// - **401 Unauthorized**: Если не прилогинились
    /// </remarks>
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
