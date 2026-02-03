using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Excursion_GPT.Infrastructure.Data;
using Excursion_GPT.Infrastructure.Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Excursion_GPT.Application.Services;

public class ModelService : IModelService
{
    private readonly AppDbContext _context;
    private readonly IMinioService _minioService;
    private readonly ILogger<ModelService> _logger;

    public ModelService(
        AppDbContext context,
        IMinioService minioService,
        ILogger<ModelService> logger)
    {
        _context = context;
        _minioService = minioService;
        _logger = logger;
    }

    public async Task<ModelUploadResponseDto> UploadModelAsync(ModelUploadRequestDto uploadDto)
    {
        _logger.LogInformation("Uploading model file: {FileName}", uploadDto.File.FileName);

        // Validate file
        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            throw new InvalidOperationException("File is empty");
        }

        // Check file size (e.g., limit to 100MB)
        const long maxFileSize = 100 * 1024 * 1024; // 100MB
        if (uploadDto.File.Length > maxFileSize)
        {
            throw new InvalidOperationException("File size exceeds limit");
        }

        try
        {
            // Generate unique model ID
            var modelId = Guid.NewGuid().ToString("N");
            var fileName = $"{modelId}_{uploadDto.File.FileName}";

            _logger.LogInformation("Uploading model with ID: {ModelId}, File: {FileName}", modelId, fileName);

            // Upload to MinIO
            using var stream = uploadDto.File.OpenReadStream();
            await _minioService.UploadFileAsync(fileName, stream, uploadDto.File.ContentType);

            // In a real application, you would save metadata to database
            // For now, we'll just return the model ID

            _logger.LogInformation("Model uploaded successfully: {ModelId}", modelId);

            return new ModelUploadResponseDto
            {
                Model = modelId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload model");
            throw new InvalidOperationException("Could not upload model");
        }
    }

    public async Task<ModelUpdateResponseDto> UpdateModelPositionAsync(string modelId, ModelUpdateRequestDto updateDto)
    {
        _logger.LogInformation("Updating model position for model ID: {ModelId}", modelId);

        // Validate model ID
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Model ID is required");
        }

        // Validate position and rotation arrays
        if (updateDto.Position == null || updateDto.Position.Count != 3)
        {
            throw new InvalidOperationException("Position must be an array of 3 numbers [x, y, z]");
        }

        if (updateDto.Rotation == null || updateDto.Rotation.Count != 3)
        {
            throw new InvalidOperationException("Rotation must be an array of 3 numbers [a, b, c]");
        }

        // Check if model exists (in a real app, check database)
        // For now, simulate not found for certain IDs
        if (modelId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Model not found");
        }

        // In a real application, you would update the model metadata in database
        // For now, return updated information

        _logger.LogInformation("Model position updated successfully: {ModelId}", modelId);

        return new ModelUpdateResponseDto
        {
            Id = modelId,
            Position = new List<double> { updateDto.Position[0], updateDto.Position[1], updateDto.Position[2] },
            Rotation = new List<double> { updateDto.Rotation[0], updateDto.Rotation[1], updateDto.Rotation[2] },
            Scale = updateDto.Scale
        };
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> GetModelFileAsync(string modelId)
    {
        _logger.LogInformation("Getting model file for model ID: {ModelId}", modelId);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Model ID is required");
        }

        // Check if model exists (in a real app, check database)
        // For now, simulate not found for certain IDs
        if (modelId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Model not found");
        }

        try
        {
            // In a real application, you would:
            // 1. Get model metadata from database
            // 2. Construct the file name from model ID
            // 3. Download from MinIO

            // For now, simulate file retrieval
            var fileName = $"{modelId}.glb"; // Assuming GLB format
            var contentType = "model/gltf-binary";

            // Create a mock stream (in real app, this would come from MinIO)
            var mockContent = $"Mock 3D model content for {modelId}";
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(mockContent));

            _logger.LogInformation("Model file retrieved successfully: {ModelId}", modelId);

            return (stream, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model file: {ModelId}", modelId);
            throw new InvalidOperationException("Failed to retrieve model file");
        }
    }

    public async Task<ModelByAddressResponseDto> GetModelByAddressAsync(ModelByAddressRequestDto request)
    {
        _logger.LogInformation("Getting model metadata by address: {Address}", request.Address);

        if (string.IsNullOrWhiteSpace(request.Address))
        {
            throw new InvalidOperationException("Address is required");
        }

        // In a real application, you would query database by address
        // For now, simulate not found for certain addresses
        if (request.Address.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Model not found");
        }

        // Return mock data based on the requirements
        return await Task.FromResult(new ModelByAddressResponseDto
        {
            ModelId = $"model_{Guid.NewGuid().ToString("N").Substring(0, 8)}",
            Address = request.Address,
            Position = new List<double> { 66.3333, 0.0, 65.4444 }, // [x, 0, z]
            Rotation = 1.5708, // 90 degrees in radians (π/2)
            Scale = 1.0
        });
    }

    public async Task SaveModelMetadataAsync(string modelId, ModelMetadataUpdateRequestDto request)
    {
        _logger.LogInformation("Saving metadata for model ID: {ModelId}", modelId);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Model ID is required");
        }

        // Check if model exists (in a real app, check database)
        // For now, simulate not found for certain IDs
        if (modelId.Contains("notfound", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Model not found");
        }

        // Validate position if provided
        if (request.Position != null && request.Position.Count != 3)
        {
            throw new InvalidOperationException("Position must be an array of 3 numbers [x, 0, z]");
        }

        // In a real application, you would:
        // 1. Validate the model exists
        // 2. Update metadata in database
        // 3. Handle polygons and address updates

        _logger.LogInformation("Model metadata saved successfully: {ModelId}", modelId);

        await Task.CompletedTask;
    }
}
