namespace Excursion_GPT.Application.DTOs;

using System.Collections.Generic;

// Node for building polygon
public record BuildingNodeDto(double Lat, double Lng);

// Request DTO for PUT /buildings - Get buildings around point
public record BuildingsAroundPointRequestDto
{
    public PositionDto Position { get; init; } = new();
    public double Distance { get; init; }
}

// Position DTO
public record PositionDto
{
    public double X { get; init; }
    public double Z { get; init; }
}

// Response DTO for PUT /buildings - Standard building
public record StandardBuildingResponseDto
{
    public string Id { get; init; } = string.Empty;
    public List<BuildingNodeDto> Nd { get; init; } = new();
}

// Response DTO for PUT /buildings - Building with model
public record ModelBuildingResponseDto
{
    public string Id { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty; // model_id
    public double Lat { get; init; }
    public double Lng { get; init; }
    public List<double> Rot { get; init; } = new(); // [0, 1.5, 0]
}

// Request DTO for PUT /buildings/address - Get building by address
public record BuildingByAddressRequestDto
{
    public string Address { get; init; } = string.Empty;
}

// Response DTO for PUT /buildings/address
public record BuildingByAddressResponseDto
{
    public string? Address { get; init; }
    public List<PositionDto> Nodes { get; init; } = new();
    public double Height { get; init; }
    public PositionDto? Position { get; init; }
    public string? ModelUrl { get; init; }
}

// Error DTOs
public record ErrorResponseDto
{
    public int Code { get; init; }
    public string Object { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
}

// Specific error DTOs for buildings
public record AuthenticationErrorDto : ErrorResponseDto
{
    public AuthenticationErrorDto()
    {
        Code = 401;
        Object = "authentification";
        Message = "Authentication should be made";
    }
}

public record RoleErrorDto : ErrorResponseDto
{
    public RoleErrorDto()
    {
        Code = 403;
        Object = "role";
        Message = "Access restricted";
    }
}

public record PositionErrorDto : ErrorResponseDto
{
    public PositionErrorDto()
    {
        Code = 406;
        Object = "position";
        Message = "Unknown terrain";
    }
}

public record TrackErrorDto : ErrorResponseDto
{
    public TrackErrorDto()
    {
        Code = 404;
        Object = "track";
        Message = "Specified track is not found";
    }
}

public record BuildingErrorDto : ErrorResponseDto
{
    public BuildingErrorDto()
    {
        Code = 404;
        Object = "building";
        Message = "Building not found";
    }
}
