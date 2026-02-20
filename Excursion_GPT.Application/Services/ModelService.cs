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

            // Handle polygons if provided
            if (updateDto.Polygons != null && updateDto.Polygons.Any())
            {
                _logger.LogInformation("Processing {PolygonCount} polygons for model {ModelId}", updateDto.Polygons.Count, modelId);

                // Clear existing polygon relationships for this model
                var existingPolygons = await _context.ModelPolygons
                    .Where(mp => mp.ModelId == modelGuid)
                    .ToListAsync();

                if (existingPolygons.Any())
                {
                    _context.ModelPolygons.RemoveRange(existingPolygons);
                    _logger.LogInformation("Removed {Count} existing polygon relationships for model {ModelId}",
                        existingPolygons.Count, modelId);
                }

                // Clear modelid from buildings that were linked to this model
                // Use a new instance for each building to update only ModelId
                var buildingIdsToClear = await _context.Buildings
                    .Where(b => b.ModelId == modelGuid)
                    .Select(b => b.Id)
                    .ToListAsync();

                foreach (var buildingId in buildingIdsToClear)
                {
                    var buildingToClear = new Domain.Entities.Building
                    {
                        Id = buildingId,
                        ModelId = null
                    };
                    _context.Buildings.Attach(buildingToClear);
                    _context.Entry(buildingToClear).Property(b => b.ModelId).IsModified = true;
                }

                // Process each polygon
                foreach (var polygonIdStr in updateDto.Polygons)
                {
                    if (Guid.TryParse(polygonIdStr, out var polygonId))
                    {
                        // Verify the polygon (building) exists
                        var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == polygonId);
                        if (!buildingExists)
                        {
                            _logger.LogWarning("Polygon (building) with ID {PolygonId} does not exist, skipping", polygonId);
                            continue;
                        }

                        // Remove any existing relationships for this polygon with OTHER models
                        var existingRelationships = await _context.ModelPolygons
                            .Where(mp => mp.PolygonId == polygonId && mp.ModelId != modelGuid)
                            .ToListAsync();

                        if (existingRelationships.Any())
                        {
                            _context.ModelPolygons.RemoveRange(existingRelationships);
                            _logger.LogInformation("Removed {Count} existing polygon relationships for polygon {PolygonId} with other models",
                                existingRelationships.Count, polygonId);
                        }

                        // Clear modelid from the building if it was pointing to a different model
                        // Use AsNoTracking to prevent entity tracking and address overwrite
                        var building = await _context.Buildings
                            .AsNoTracking()
                            .FirstOrDefaultAsync(b => b.Id == polygonId);
                        if (building != null && building.ModelId.HasValue && building.ModelId.Value != modelGuid)
                        {
                            // Create a new instance to update only ModelId
                            var buildingToClear = new Domain.Entities.Building
                            {
                                Id = polygonId,
                                ModelId = null
                            };
                            _context.Buildings.Attach(buildingToClear);
                            _context.Entry(buildingToClear).Property(b => b.ModelId).IsModified = true;
                        }

                        // Create new relationship
                        var modelPolygon = new Domain.Entities.ModelPolygon
                        {
                            ModelId = modelGuid,
                            PolygonId = polygonId
                        };

                        await _context.ModelPolygons.AddAsync(modelPolygon);
                        _logger.LogInformation("Added polygon relationship: Model {ModelId} -> Polygon {PolygonId}", modelId, polygonId);

                        // Update model's buildingid to point to the first polygon in the list
                        if (updateDto.Polygons.IndexOf(polygonIdStr) == 0)
                        {
                            model.BuildingId = polygonId;
                            _logger.LogInformation("Updated model.BuildingId to {PolygonId}", polygonId);
                        }

                        // Update building's modelid to point to this model
                        // Create a new instance to update only ModelId
                        var buildingToUpdate = new Domain.Entities.Building
                        {
                            Id = polygonId,
                            ModelId = modelGuid
                        };
                        _context.Buildings.Attach(buildingToUpdate);
                        _context.Entry(buildingToUpdate).Property(b => b.ModelId).IsModified = true;
                        _logger.LogInformation("Updated building.ModelId to {ModelId} for building {BuildingId}", modelId, polygonId);
                    }
                    else
                    {
                        _logger.LogWarning("Invalid polygon ID format: {PolygonId}, skipping", polygonIdStr);
                    }
                }
            }

            // Save all changes to database
            await _context.SaveChangesAsync();
            _logger.LogInformation("Model position and metadata updated successfully: {ModelId}", modelId);

            return new ModelUpdateResponseDto
            {
                Id = modelId,
                Position = model.Position,
                Rotation = model.Rotation,
                Scale = model.Scale,
                Polygons = updateDto.Polygons,
                Address = updateDto.Address
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
        _logger.LogInformation("DEBUG: SaveModelMetadataAsync called for model {ModelId}", modelId);

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

        // Find model in database with related data
        var model = await _context.Models
            .Include(m => m.Building)
            .Include(m => m.ModelPolygons)
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
            _logger.LogInformation("DEBUG: Processing request with polygons: {HasPolygons}", request.Polygons != null);

            // STEP 1: Handle polygons if provided
            if (request.Polygons != null && request.Polygons.Any())
            {
                _logger.LogInformation("Processing {PolygonCount} polygons for model {ModelId}", request.Polygons.Count, modelId);
                _logger.LogInformation("DEBUG: Found {PolygonCount} polygons to process", request.Polygons.Count);

                // Clear existing polygon relationships for this model
                var existingPolygons = model.ModelPolygons.ToList();
                if (existingPolygons.Any())
                {
                    _logger.LogInformation("DEBUG: Removing {Count} existing polygon relationships", existingPolygons.Count);
                    _context.ModelPolygons.RemoveRange(existingPolygons);
                    _logger.LogInformation("Removed {Count} existing polygon relationships for model {ModelId}",
                        existingPolygons.Count, modelId);
                }

                // Process each polygon
                foreach (var polygonIdStr in request.Polygons)
                {
                    if (Guid.TryParse(polygonIdStr, out var polygonId))
                    {
                        // Verify the polygon (building) exists
                        var buildingExists = await _context.Buildings.AnyAsync(b => b.Id == polygonId);
                        if (!buildingExists)
                        {
                            _logger.LogWarning("Polygon (building) with ID {PolygonId} does not exist, skipping", polygonId);
                            continue;
                        }

                        // CRITICAL: Remove any existing relationships for this polygon with OTHER models
                        // This ensures one polygon has only one model
                        _logger.LogInformation("DEBUG: Checking for existing relationships for polygon {PolygonId}", polygonId);
                        var existingRelationships = await _context.ModelPolygons
                            .Where(mp => mp.PolygonId == polygonId && mp.ModelId != modelGuid)
                            .ToListAsync();

                        if (existingRelationships.Any())
                        {
                            _logger.LogInformation("DEBUG: Found {Count} existing relationships to remove", existingRelationships.Count);
                            _context.ModelPolygons.RemoveRange(existingRelationships);
                            _logger.LogInformation("Removed {Count} existing polygon relationships for polygon {PolygonId} with other models",
                                existingRelationships.Count, polygonId);
                        }
                        else
                        {
                            _logger.LogInformation("DEBUG: No existing relationships found for polygon {PolygonId}", polygonId);
                        }

                        // Clear modelId from the building if it was pointing to a different model
                        _logger.LogInformation("DEBUG: Getting building {PolygonId} to update modelid", polygonId);
                        var buildingToUpdate = await _context.Buildings.FindAsync(polygonId);
                        if (buildingToUpdate != null)
                        {
                            _logger.LogInformation("DEBUG: Building found. Current modelid: {ModelId}", buildingToUpdate.ModelId);
                            if (buildingToUpdate.ModelId.HasValue && buildingToUpdate.ModelId.Value != modelGuid)
                            {
                                _logger.LogInformation("DEBUG: Clearing modelid from building {PolygonId}", polygonId);
                                buildingToUpdate.ModelId = null;
                                _context.Buildings.Update(buildingToUpdate);
                                _logger.LogInformation("Cleared modelId from building {BuildingId} that was pointing to other model", polygonId);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("DEBUG: Building {PolygonId} not found", polygonId);
                        }

                        // Create new relationship
                        _logger.LogInformation("DEBUG: Creating new model_polygons relationship");
                        var modelPolygon = new Domain.Entities.ModelPolygon
                        {
                            ModelId = modelGuid,
                            PolygonId = polygonId
                        };

                        await _context.ModelPolygons.AddAsync(modelPolygon);
                        _logger.LogInformation("DEBUG: Added model_polygons relationship: {ModelGuid} -> {PolygonId}", modelGuid, polygonId);
                        _logger.LogInformation("Added polygon relationship: Model {ModelId} -> Polygon {PolygonId}", modelId, polygonId);

                        // Update model's buildingid to point to this polygon (first polygon is primary)
                        if (model.BuildingId != polygonId)
                        {
                            _logger.LogInformation("DEBUG: Updating model.BuildingId from {OldBuildingId} to {PolygonId}", model.BuildingId, polygonId);
                            model.BuildingId = polygonId;
                            _logger.LogInformation("Updated model.BuildingId to {PolygonId}", polygonId);
                        }
                        else
                        {
                            _logger.LogInformation("DEBUG: model.BuildingId already set to {PolygonId}", polygonId);
                        }

                        // Update building's modelid to point to this model
                        if (buildingToUpdate != null)
                        {
                            _logger.LogInformation("DEBUG: Setting building.ModelId to {ModelGuid}", modelGuid);
                            buildingToUpdate.ModelId = modelGuid;
                            _context.Buildings.Update(buildingToUpdate);
                            _logger.LogInformation("Updated building.ModelId to {ModelId} for building {BuildingId}", modelId, polygonId);
                        }
                        else
                        {
                            _logger.LogInformation("DEBUG: buildingToUpdate is null, cannot set modelid");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Invalid polygon ID format: {PolygonId}, skipping", polygonIdStr);
                    }
                }
            }

            // Update building position and other metadata
            var buildingToUpdateMetadata = model.Building;
            if (buildingToUpdateMetadata != null)
            {
                // Create a new instance with only the fields we want to update
                var buildingUpdate = new Domain.Entities.Building
                {
                    Id = buildingToUpdateMetadata.Id
                };

                if (request.Position != null)
                {
                    buildingUpdate.X = request.Position[0];
                    buildingUpdate.Z = request.Position[2]; // Position[1] is 0 (y-axis)
                }

                if (request.Rotation.HasValue)
                {
                    // Convert single rotation angle to [x, y, z] format
                    // Assuming rotation around Y-axis (vertical axis)
                    buildingUpdate.Rotation = new List<double> { 0, request.Rotation.Value, 0 };
                }

                _context.Buildings.Attach(buildingUpdate);

                // Only mark specific fields as modified to prevent overwriting address
                if (request.Position != null)
                {
                    _context.Entry(buildingUpdate).Property(b => b.X).IsModified = true;
                    _context.Entry(buildingUpdate).Property(b => b.Z).IsModified = true;
                }
                if (request.Rotation.HasValue)
                {
                    _context.Entry(buildingUpdate).Property(b => b.Rotation).IsModified = true;
                }
                // Explicitly mark Address as not modified to prevent overwriting
                _context.Entry(buildingUpdate).Property(b => b.Address).IsModified = false;

                // Note: Building address is not updated here to preserve original address data
                // Address in request is for informational purposes only
            }

            // STEP 3: Update model fields
            bool modelUpdated = false;

            // Update model position if provided
            if (request.Position != null)
            {
                model.Position = request.Position;
                modelUpdated = true;
            }

            // Update model rotation if provided
            if (request.Rotation.HasValue)
            {
                // Convert single rotation angle to [x, y, z] format for model
                model.Rotation = new List<double> { 0, request.Rotation.Value, 0 };
                modelUpdated = true;
            }

            // Update model scale if provided
            if (request.Scale.HasValue)
            {
                model.Scale = request.Scale.Value;
                modelUpdated = true;
            }

            // Update model in database if any fields were changed
            if (modelUpdated)
            {
                _context.Models.Update(model);
            }

            // Save all changes to database
            _logger.LogInformation("DEBUG: Saving changes to database");
            await _context.SaveChangesAsync();
            _logger.LogInformation("DEBUG: Changes saved successfully");

            _logger.LogInformation("Model metadata saved successfully: {ModelId}", modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save model metadata: {ModelId}", modelId);
            throw new InvalidOperationException("Failed to save model metadata");
        }
    }
}
