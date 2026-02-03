using Excursion_GPT.Application.DTOs;
using Excursion_GPT.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Excursion_GPT.Controllers;

[ApiController]
[Route("models")]
public class ModelsController : ControllerBase
{
    private readonly IModelService _modelService;
    private readonly ILogger<ModelsController> _logger;

    public ModelsController(IModelService modelService, ILogger<ModelsController> logger)
    {
        _modelService = modelService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a model for a building
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin,Creator")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelUploadResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(BuildingErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackErrorDto))]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge, Type = typeof(UploadErrorDto))]
    public async Task<ActionResult<ModelUploadResponseDto>> UploadModel([FromForm] ModelUploadRequestDto uploadDto)
    {
        _logger.LogInformation("Uploading model file: {FileName}", uploadDto.File.FileName);

        try
        {
            var response = await _modelService.UploadModelAsync(uploadDto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("building"))
        {
            return NotFound(new BuildingErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackErrorDto());
        }
        catch (Exception ex) when (ex.Message.Contains("upload") || ex is IOException)
        {
            return StatusCode(413, new UploadErrorDto());
        }
    }

    /// <summary>
    /// Upload a model for a building (alternative route: POST /upload)
    /// </summary>
    [HttpPost("/upload")]
    [Authorize(Roles = "Admin,Creator")]
    [DisableRequestSizeLimit]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelUploadResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(BuildingErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackErrorDto))]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge, Type = typeof(UploadErrorDto))]
    public async Task<ActionResult<ModelUploadResponseDto>> UploadModelDirect([FromForm] ModelUploadRequestDto uploadDto)
    {
        _logger.LogInformation("Uploading model file via direct route: {FileName}", uploadDto.File.FileName);

        try
        {
            var response = await _modelService.UploadModelAsync(uploadDto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AuthenticationErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("role"))
        {
            return StatusCode(403, new RoleErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("building"))
        {
            return NotFound(new BuildingErrorDto());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("track"))
        {
            return NotFound(new TrackErrorDto());
        }
        catch (Exception ex) when (ex.Message.Contains("upload") || ex is IOException)
        {
            return StatusCode(413, new UploadErrorDto());
        }
    }

    /// <summary>
    /// Update model position on the map
    /// </summary>
    [HttpPut("{modelId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelUpdateResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult<ModelUpdateResponseDto>> UpdateModelPosition(
        string modelId,
        [FromBody] ModelUpdateRequestDto updateDto)
    {
        _logger.LogInformation("Updating model position for model ID: {ModelId}", modelId);

        try
        {
            var updatedModel = await _modelService.UpdateModelPositionAsync(modelId, updateDto);
            return Ok(updatedModel);
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
            return NotFound(new ModelErrorDto());
        }
    }

    /// <summary>
    /// Update model position on the map (alternative route: PUT /model/:model_id)
    /// </summary>
    [HttpPut("/model/{modelId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelUpdateResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult<ModelUpdateResponseDto>> UpdateModelPositionDirect(
        string modelId,
        [FromBody] ModelUpdateRequestDto updateDto)
    {
        _logger.LogInformation("Updating model position via direct route for model ID: {ModelId}", modelId);

        try
        {
            var updatedModel = await _modelService.UpdateModelPositionAsync(modelId, updateDto);
            return Ok(updatedModel);
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
            return NotFound(new ModelErrorDto());
        }
    }

    /// <summary>
    /// Get model binary file from server
    /// </summary>
    [HttpGet("{modelId}")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult> GetModelFile(string modelId)
    {
        _logger.LogInformation("Getting model file for model ID: {ModelId}", modelId);

        try
        {
            var (stream, contentType, fileName) = await _modelService.GetModelFileAsync(modelId);
            return File(stream, contentType, fileName);
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
            return NotFound(new ModelErrorDto());
        }
    }

    /// <summary>
    /// Get model metadata by address
    /// </summary>
    [HttpPut("address")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelByAddressResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(MetadataErrorDto))]
    public async Task<ActionResult<ModelByAddressResponseDto>> GetModelByAddress(
        [FromBody] ModelByAddressRequestDto request)
    {
        _logger.LogInformation("Getting model metadata by address: {Address}", request.Address);

        try
        {
            var metadata = await _modelService.GetModelByAddressAsync(request);
            return Ok(metadata);
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
            return NotFound(new MetadataErrorDto());
        }
    }

    /// <summary>
    /// Save model metadata
    /// </summary>
    [HttpPatch("{modelId}")]
    [Authorize(Roles = "Admin,Creator")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult> SaveModelMetadata(
        string modelId,
        [FromBody] ModelMetadataUpdateRequestDto request)
    {
        _logger.LogInformation("Saving metadata for model ID: {ModelId}", modelId);

        try
        {
            await _modelService.SaveModelMetadataAsync(modelId, request);
            return Ok();
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
            return NotFound(new ModelErrorDto());
        }
    }

    /// <summary>
    /// Get model from backend by ID (same as GetModelFile but with different route)
    /// This endpoint is mentioned in the requirements as GET /model/:model_id
    /// </summary>
    [HttpGet("model/{modelId}")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult> GetModelById(string modelId)
    {
        _logger.LogInformation("Getting model by ID: {ModelId}", modelId);

        try
        {
            var (stream, contentType, fileName) = await _modelService.GetModelFileAsync(modelId);
            return File(stream, contentType, fileName);
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
            return NotFound(new ModelErrorDto());
        }
    }

    /// <summary>
    /// Get model from backend by ID (alternative route: GET /model/:model_id)
    /// </summary>
    [HttpGet("/model/{modelId}")]
    [Authorize(Roles = "Admin,Creator,User")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ModelErrorDto))]
    public async Task<ActionResult> GetModelByIdDirect(string modelId)
    {
        _logger.LogInformation("Getting model by ID via direct route: {ModelId}", modelId);

        try
        {
            var (stream, contentType, fileName) = await _modelService.GetModelFileAsync(modelId);
            return File(stream, contentType, fileName);
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
            return NotFound(new ModelErrorDto());
        }
    }
}
