using Excursion_GPT.Application.DTOs;

namespace Excursion_GPT.Application.Interfaces;

public interface IModelService
{
    /// <summary>
    /// Upload a model for a building
    /// </summary>
    /// <param name="uploadDto">Model upload request with file</param>
    /// <returns>Upload response with model ID</returns>
    Task<ModelUploadResponseDto> UploadModelAsync(ModelUploadRequestDto uploadDto);

    /// <summary>
    /// Update model position on the map
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="updateDto">Update request with position, rotation, and scale</param>
    /// <returns>Updated model information</returns>
    Task<ModelUpdateResponseDto> UpdateModelPositionAsync(string modelId, ModelUpdateRequestDto updateDto);

    /// <summary>
    /// Get model binary file from server
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <returns>File stream, content type, and file name</returns>
    Task<(Stream Stream, string ContentType, string FileName)> GetModelFileAsync(string modelId);

    /// <summary>
    /// Get model metadata by address
    /// </summary>
    /// <param name="request">Request with address to search</param>
    /// <returns>Model metadata including position, rotation, and scale</returns>
    Task<ModelByAddressResponseDto> GetModelByAddressAsync(ModelByAddressRequestDto request);

    /// <summary>
    /// Save model metadata
    /// </summary>
    /// <param name="modelId">Model ID</param>
    /// <param name="request">Metadata update request</param>
    Task SaveModelMetadataAsync(string modelId, ModelMetadataUpdateRequestDto request);
}
