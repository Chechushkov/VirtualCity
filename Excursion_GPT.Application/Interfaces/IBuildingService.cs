using Excursion_GPT.Application.DTOs;

namespace Excursion_GPT.Application.Interfaces;

public interface IBuildingService
{
    /// <summary>
    /// Get buildings around a point with coordinates x and z, at distance
    /// </summary>
    /// <param name="request">Request with position and distance</param>
    /// <returns>List of buildings (standard and with models)</returns>
    Task<List<object>> GetBuildingsAroundPointAsync(BuildingsAroundPointRequestDto request);

    /// <summary>
    /// Get building coordinates by address
    /// </summary>
    /// <param name="request">Request with address to search</param>
    /// <returns>Building information including coordinates and model URL</returns>
    Task<BuildingByAddressResponseDto> GetBuildingByAddressAsync(BuildingByAddressRequestDto request);
}
