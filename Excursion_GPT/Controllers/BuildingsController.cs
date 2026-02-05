using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Excursion_GPT.Controllers;

[ApiController]
[Route("buildings")]
public class BuildingsController : ControllerBase
{
    private readonly IBuildingService _buildingService;
    private readonly ILogger<BuildingsController> _logger;

    public BuildingsController(IBuildingService buildingService, ILogger<BuildingsController> logger)
    {
        _buildingService = buildingService;
        _logger = logger;
    }

    /// <summary>
    /// Получить с бэкенда здания вокруг точки
    /// </summary>
    /// <remarks>
    /// Получить здания вокруг точки с координатами x и z, на расстоянии distance.
    /// Судя по описанным ролям, пользователи со свободным доступом к карте должны иметь возможность использовать именно этот запрос.
    ///
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "position": {
    ///     "x": number,
    ///     "z": number
    ///   },
    ///   "distance": number
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// [
    ///   // По одному на каждое стандартное здание
    ///   { "id": "234234", "nodes": [{"x": -6736606.72045857, "z": 7713514.742933013}, ...]},
    ///   ...
    ///   // Для зданий с указанной моделькой
    ///   { "id": "234235", "model": "model_id", "x": -6736586.72045857, "z": 7713534.742933013, "rot": [0, 1.5, 0]},
    /// ]
    /// ```
    ///
    /// **Ошибки:**
    /// - **401 Unauthorized**: Универсальная ошибка, если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// - **406 Not Acceptable**: Если запрашиваем северный полюс
    /// - **404 Not Found**: Если экскурсия не создана/удалена
    /// </remarks>
    /// <param name="request">Запрос с позицией и расстоянием</param>
    /// <returns>Список зданий вокруг указанной точки</returns>
    [HttpPut]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<object>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackErrorDto))]
    [ProducesResponseType(StatusCodes.Status406NotAcceptable, Type = typeof(PositionErrorDto))]
    public async Task<ActionResult<List<object>>> GetBuildingsAroundPoint(
        [FromBody] BuildingsAroundPointRequestDto request)
    {
        _logger.LogInformation(
            "Getting buildings around point ({X}, {Z}) at distance {Distance}",
            request.Position.X, request.Position.Z, request.Distance);

        try
        {
            var buildings = await _buildingService.GetBuildingsAroundPointAsync(request);
            return Ok(buildings);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("terrain") || ex.Message.Contains("position"))
        {
            return StatusCode(406, new PositionErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackErrorDto());
        }
    }

    /// <summary>
    /// Получить с бэкенда координаты здания с указанным адресом
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "address": "string" // Что искать
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "address": "string",
    ///   "nodes": [{ "x": number, "z": number }, ...],
    ///   "height": number,
    ///   "position": { "x": number, "z": number },
    ///   "modelUrl": "string"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если здание не найдено
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
    /// <param name="request">Запрос с адресом для поиска</param>
    /// <returns>Информация о здании включая координаты и URL модели</returns>
    [HttpPut("address")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BuildingByAddressResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(BuildingErrorDto))]
    public async Task<ActionResult<BuildingByAddressResponseDto>> GetBuildingByAddress(
        [FromBody] BuildingByAddressRequestDto request)
    {
        _logger.LogInformation("Getting building by address: {Address}", request.Address);

        try
        {
            var building = await _buildingService.GetBuildingByAddressAsync(request);
            return Ok(building);
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
            return NotFound(new BuildingErrorDto());
        }
    }

    /// <summary>
    /// Получить точку старта для экскурсии
    /// </summary>
    /// <remarks>
    /// Возвращает фиксированную точку старта в координатах Web Mercator для начала экскурсии.
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "x": -6736606.72045857,
    ///   "z": 7713514.742933013
    /// }
    /// ```
    /// </remarks>
    /// <returns>Координаты точки старта в системе Web Mercator</returns>
    [HttpGet("start")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StartPointResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(StartPointErrorDto))]
    public async Task<ActionResult<StartPointResponse>> GetStartPoint()
    {
        _logger.LogInformation("Getting start point for excursion");

        try
        {
            var startPoint = new StartPointResponse
            {
                X = -6736606.72045857,
                Z = 7713514.742933013
            };

            return Ok(startPoint);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting start point");
            return StatusCode(500, new StartPointErrorDto());
        }
    }
}

/// <summary>
/// DTO для ответа с точкой старта
/// </summary>
public class StartPointResponse
{
    /// <summary>
    /// X координата в системе Web Mercator
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Z координата в системе Web Mercator
    /// </summary>
    public double Z { get; set; }
}
