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

        // Check file size (e.g., limit to 200MB)
        const long maxFileSize = 200 * 1024 * 1024; // 200MB
        if (uploadDto.File.Length > maxFileSize)
        {
            throw new InvalidOperationException("File size exceeds limit");
        }

        try
        {
            // Generate unique model ID
            var modelId = Guid.NewGuid();
            var fileName = $"{modelId}_{uploadDto.File.FileName}";

            _logger.LogInformation("Uploading model with ID: {ModelId}, File: {FileName}", modelId, fileName);

            // Upload to MinIO
            using var stream = uploadDto.File.OpenReadStream();
            await _minioService.UploadFileAsync(fileName, stream, uploadDto.File.ContentType);

            // Save file metadata to database
            var modelFile = new Domain.Entities.ModelFile
            {
                Id = modelId,
                MinioObjectName = fileName,
                OriginalFileName = uploadDto.File.FileName,
                ContentType = uploadDto.File.ContentType,
                FileSize = uploadDto.File.Length,
                UploadedAt = DateTime.UtcNow
            };

            // Get or create a default track for the model
            var defaultTrack = await _context.Tracks.FirstOrDefaultAsync();
            if (defaultTrack == null)
            {
                // Get first user to use as creator
                var firstUser = await _context.Users.FirstOrDefaultAsync();
                if (firstUser == null)
                {
                    throw new InvalidOperationException("No users found in database. Cannot create model without a track creator.");
                }

                // Create a default track if none exists
                defaultTrack = new Domain.Entities.Track
                {
                    Id = Guid.NewGuid(),
                    Name = "Default Track",
                    CreatorId = firstUser.Id
                };
                await _context.Tracks.AddAsync(defaultTrack);
            }

            // Create a placeholder building for the model (without ModelId initially to avoid circular dependency)
            var placeholderBuilding = new Domain.Entities.Building
            {
                Id = Guid.NewGuid(),
                X = 0,
                Z = 0,
                Address = $"Model {modelId.ToString("N").Substring(0, 8)}",
                Height = 10.0,
                ModelId = null, // Will be set after model is created
                Rotation = new List<double> { 0, 0, 0 }
            };

            // Create model record with default position/rotation/scale
            var model = new Domain.Entities.Model
            {
                Id = modelId,
                BuildingId = placeholderBuilding.Id,
                TrackId = defaultTrack.Id,
                MinioObjectName = fileName,
                Position = new List<double> { 0, 0, 0 },
                Rotation = new List<double> { 0, 0, 0 },
                Scale = 1.0
            };

            // Save building first (without ModelId reference)
            await _context.Buildings.AddAsync(placeholderBuilding);
            await _context.SaveChangesAsync();

            // Save model file and model
            await _context.ModelFiles.AddAsync(modelFile);
            await _context.Models.AddAsync(model);
            await _context.SaveChangesAsync();

            // Now update the building with the ModelId
            placeholderBuilding.ModelId = modelId;
            _context.Buildings.Update(placeholderBuilding);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Model uploaded successfully: {ModelId}", modelId);

            return new ModelUploadResponseDto
            {
                Model = modelId.ToString("N")
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

        // Parse model ID from string
        if (!Guid.TryParse(modelId, out var modelGuid))
        {
            _logger.LogWarning("Invalid model ID format: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        // Find model in database
        var model = await _context.Models.FindAsync(modelGuid);
        if (model == null)
        {
            _logger.LogWarning("Model not found in database for model ID: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        try
        {
            // Update model position, rotation, and scale
            model.Position = updateDto.Position;
            model.Rotation = updateDto.Rotation;
            model.Scale = updateDto.Scale;

            // Save changes to database
            _context.Models.Update(model);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Model position updated successfully: {ModelId}", modelId);

            return new ModelUpdateResponseDto
            {
                Id = modelId,
                Position = model.Position,
                Rotation = model.Rotation,
                Scale = model.Scale
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update model position: {ModelId}", modelId);
            throw new InvalidOperationException("Failed to update model position");
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> GetModelFileAsync(string modelId)
    {
        _logger.LogInformation("Getting model file for model ID: {ModelId}", modelId);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new InvalidOperationException("Model ID is required");
        }

        // Parse model ID from string
        if (!Guid.TryParse(modelId, out var modelGuid))
        {
            _logger.LogWarning("Invalid model ID format: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        // Find model file metadata in database
        var modelFile = await _context.ModelFiles.FindAsync(modelGuid);
        if (modelFile == null)
        {
            _logger.LogWarning("Model file not found in database for model ID: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        try
        {
            _logger.LogInformation("Found model file in database: {ObjectName} for model ID: {ModelId}", modelFile.MinioObjectName, modelId);

            // Download the file from MinIO
            var (stream, contentType) = await _minioService.DownloadFileAsync(modelFile.MinioObjectName);

            _logger.LogInformation("Model file retrieved successfully from MinIO: {ModelId}, File: {FileName}", modelId, modelFile.OriginalFileName);

            return (stream, contentType, modelFile.OriginalFileName);
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

        // Parse model ID from string
        if (!Guid.TryParse(modelId, out var modelGuid))
        {
            _logger.LogWarning("Invalid model ID format: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        // Find model in database
        var model = await _context.Models
            .Include(m => m.Building)
            .FirstOrDefaultAsync(m => m.Id == modelGuid);

        if (model == null)
        {
            _logger.LogWarning("Model not found in database for model ID: {ModelId}", modelId);
            throw new InvalidOperationException("Model not found");
        }

        // Validate position if provided
        if (request.Position != null && request.Position.Count != 3)
        {
            throw new InvalidOperationException("Position must be an array of 3 numbers [x, 0, z]");
        }

        try
        {
            // Update building associated with the model if it exists
            if (model.Building != null)
            {
                // Update building position (X and Z coordinates)
                if (request.Position != null)
                {
                    model.Building.X = request.Position[0];
                    model.Building.Z = request.Position[2]; // Position[1] is 0 (y-axis)
                }

                // Update building address if provided
                if (!string.IsNullOrWhiteSpace(request.Address))
                {
                    model.Building.Address = request.Address;
                }

                // Update building rotation if provided
                if (request.Rotation.HasValue)
                {
                    // Convert single rotation angle to [x, y, z] format
                    // Assuming rotation around Y-axis (vertical axis)
                    model.Building.Rotation = new List<double> { 0, request.Rotation.Value, 0 };
                }

                // Update polygons if provided
                if (request.Polygons != null)
                {
                    // Convert polygons list to JSON string
                    var polygonsJson = System.Text.Json.JsonSerializer.Serialize(request.Polygons);
                    model.Building.NodesJson = polygonsJson;
                }

                _context.Buildings.Update(model.Building);
            }

            // Update model scale if provided
            if (request.Scale.HasValue)
            {
                model.Scale = request.Scale.Value;
                _context.Models.Update(model);
            }

            // Save all changes to database
            await _context.SaveChangesAsync();

            _logger.LogInformation("Model metadata saved successfully: {ModelId}", modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save model metadata: {ModelId}", modelId);
            throw new InvalidOperationException("Failed to save model metadata");
        }
    }
}
