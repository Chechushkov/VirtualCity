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
    /// Get buildings around a point with coordinates x and z, at distance
    /// </summary>
    /// <param name="request">Request with position and distance</param>
    /// <returns>List of buildings around the specified point</returns>
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
    /// Get building coordinates by address
    /// </summary>
    /// <param name="request">Request with address to search</param>
    /// <returns>Building information including coordinates and model URL</returns>
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
