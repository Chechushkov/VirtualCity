using System.Collections.Generic;

namespace Excursion_GPT.Application.DTOs;

// Request DTO for POST /tracks - Create new track
public record TrackCreateRequestDto
{
    public string Name { get; init; } = string.Empty;
}

// Response DTO for POST /tracks
public record TrackCreateResponseDto
{
    public string Id { get; init; } = string.Empty; // track_id
    public string Name { get; init; } = string.Empty;
}

// Response DTO for GET /tracks/ - Get all tracks
public record TrackListItemDto
{
    public string Id { get; init; } = string.Empty; // track_id
    public string Name { get; init; } = string.Empty; // track_name
}

// Response DTO for GET /tracks/:track_id - Get track with all points
public record TrackDetailsDto
{
    public string Id { get; init; } = string.Empty; // track_id
    public string Name { get; init; } = string.Empty; // track_name
    public List<PointDto> Points { get; init; } = new();
}

// Point DTO for track details
public record PointDto
{
    public string Id { get; init; } = string.Empty; // point_id
    public string Name { get; init; } = string.Empty; // point_name
    public double Lat { get; init; }
    public double Lng { get; init; }
    public string Type { get; init; } = string.Empty; // point_type
    public List<double> Position { get; init; } = new(); // [x, y, z]
    public List<double> Rotation { get; init; } = new(); // [a, b, c]
    // Additional properties for point behavior restrictions
    public bool? RotationRestricted { get; init; }
    public bool? TiltRestricted { get; init; }
    public bool? MovementRestricted { get; init; }
}

// Request DTO for POST /tracks/:track_id - Add point to track
public record PointCreateRequestDto
{
    public string Name { get; init; } = string.Empty; // point_name
    public string Type { get; init; } = string.Empty; // point_type
    public List<double> Position { get; init; } = new(); // [x, y, z]
    public List<double> Rotation { get; init; } = new(); // [a, b, c]
}

// Response DTO for POST /tracks/:track_id
public record PointCreateResponseDto
{
    public string Id { get; init; } = string.Empty; // point_id
}

// Request DTO for PUT /tracks/:track_id/:point_id - Update point
public record PointUpdateRequestDto
{
    public string? Name { get; init; } // point_name
    public string? Type { get; init; } // point_type
    public List<double>? Position { get; init; } // [x, y, z]
    public List<double>? Rotation { get; init; } // [a, b, c]
}

// Error DTOs for tracks
public record TrackNotFoundErrorDto : ErrorResponseDto
{
    public TrackNotFoundErrorDto()
    {
        Code = 404;
        Object = "track";
        Message = "Track not found";
    }
}

public record PointErrorDto : ErrorResponseDto
{
    public PointErrorDto()
    {
        Code = 404;
        Object = "point";
        Message = "Point not found";
    }
}

// Note: AuthenticationErrorDto and RoleErrorDto are already defined in BuildingNodeDto.cs
// and can be reused for tracks and points endpoints
