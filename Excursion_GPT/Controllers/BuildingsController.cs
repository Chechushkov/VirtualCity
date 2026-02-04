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
    ///   { "id": "234234", "nd": [{"lat": 66.3333, "lng": 65.4444}, ...]},
    ///   ...
    ///   // Для зданий с указанной моделькой
    ///   { "id": "234235", "model": "model_id", "lat": 67.3333, "lng": 68.4444, "rot": [0, 1.5, 0]},
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

            // Create a mixed list of standard and model buildings
            var result = new List<object>();
            foreach (var building in buildings)
            {
                if (building is StandardBuildingResponseDto standardBuilding)
                {
                    result.Add(new
                    {
                        id = standardBuilding.Id,
                        nd = standardBuilding.Nd.Select(n => new { lat = n.Lat, lng = n.Lng })
                    });
                }
                else if (building is ModelBuildingResponseDto modelBuilding)
                {
                    result.Add(new
                    {
                        id = modelBuilding.Id,
                        model = modelBuilding.Model,
                        lat = modelBuilding.Lat,
                        lng = modelBuilding.Lng,
                        rot = modelBuilding.Rot
                    });
                }
            }

            return Ok(result);
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
}
