using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Excursion_GPT.Application.DTOs;

// Request DTO for POST /upload - Upload model for building
public record ModelUploadRequestDto
{
    public IFormFile File { get; init; } = null!;
}

// Response DTO for POST /upload
public record ModelUploadResponseDto
{
    public string Model { get; init; } = string.Empty; // model_id
}

// Request DTO for PUT /model/:model_id - Update model position
public record ModelUpdateRequestDto
{
    public List<double> Position { get; init; } = new(); // [x, y, z]
    public List<double> Rotation { get; init; } = new(); // [a, b, c]
    public double Scale { get; init; } = 1.0;
    public List<string>? Polygons { get; init; } // [polygon1Id, polygon2Id, ...]
    public string? Address { get; init; }
}

// Response DTO for PUT /model/:model_id
public record ModelUpdateResponseDto
{
    public string Id { get; init; } = string.Empty; // model_id
    public List<double> Position { get; init; } = new(); // [lat, y, lng]
    public List<double> Rotation { get; init; } = new(); // [a, b, c]
    public double Scale { get; init; } = 1.0;
    public List<string>? Polygons { get; init; } // [polygon1Id, polygon2Id, ...]
    public string? Address { get; init; }
}

// Request DTO for PUT /models/address - Get model metadata by address
public record ModelByAddressRequestDto
{
    public string Address { get; init; } = string.Empty;
}

// Response DTO for PUT /models/address
public record ModelByAddressResponseDto
{
    public string ModelId { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public List<double> Position { get; init; } = new(); // [x, 0, z]
    public double Rotation { get; init; } // angle in radians
    public double Scale { get; init; } = 1.0;
}

// Request DTO for PATCH /models/:model_id - Save model metadata
public record ModelMetadataUpdateRequestDto
{
    public List<double>? Position { get; init; } // [x, 0, z]
    public double? Rotation { get; init; } // rotationAngle
    public double? Scale { get; init; }
    public List<string>? Polygons { get; init; } // [polygon1Id, polygon2Id, ...]
    public string? Address { get; init; }
    public Guid? BuildingId { get; init; } // Optional: link model to existing building
}

// Error DTOs for models
public record UploadErrorDto : ErrorResponseDto
{
    public UploadErrorDto()
    {
        Code = 413;
        Object = "upload";
        Message = "Could not upload model";
    }
}

public record ModelErrorDto : ErrorResponseDto
{
    public ModelErrorDto()
    {
        Code = 404;
        Object = "model";
        Message = "Model not found";
    }
}

public record MetadataErrorDto : ErrorResponseDto
{
    public MetadataErrorDto()
    {
        Code = 404;
        Object = "metadata";
        Message = "Model not found";
    }
}
