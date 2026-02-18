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
    /// Загрузить на сервер модельку для здания (альтернативный маршрут: POST /upload)
    /// </summary>
    /// <remarks>
    /// **Параметры запроса:**
    /// - `file`: Бинарный файл модели
    /// - `polygons`: [опционально] Массив идентификаторов полигонов ["polygon1Id", "polygon2Id", ...]
    /// - `address`: [опционально] Адрес здания
    ///
    /// **Примечание:** Можно указать либо `polygons` (идентификаторы полигонов), либо `address` (адрес здания), либо оба параметра. Если не указано ни одного, будет создано здание с placeholder-адресом.
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "model": "model_id"
    /// }
    /// ```
    /// Если в ответе не пришел ключ model (или получили не 2xx), значит, загрузить не удалось.
    ///
    /// **Ошибки:**
    /// - **413 Payload Too Large**: Если загрузка не удалась (проблемы с хранилищем, еще что-нибудь)
    /// - **404 Not Found**: Если указанное здание не найдено
    /// - **404 Not Found**: Если указанная экскурсия не обнаружена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено загружать
    /// </remarks>
    [HttpPost("/upload")]
    [Authorize(Roles = "Admin,Creator")]
    [DisableRequestSizeLimit]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ModelUploadResponseDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(AuthenticationErrorDto))]
    [ProducesResponseType(StatusCodes.Status403Forbidden, Type = typeof(RoleErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(BuildingErrorDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(TrackErrorDto))]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge, Type = typeof(UploadErrorDto))]
    public async Task<ActionResult<ModelUploadResponseDto>> UploadModelDirect([FromForm] ModelUploadRequestDto uploadDto)
    {
        if (uploadDto.File == null || uploadDto.File.Length == 0)
        {
            return BadRequest("File is required");
        }

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
    /// Изменить положение модели на карте
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "position": [x, y, z],
    ///   "rotation": [a, b, c],
    ///   "scale": number
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "model_id",
    ///   "position": [lat, y, lng],
    ///   "rotation": [a, b, c],
    ///   "scale": 1.0
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено изменять
    /// </remarks>
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
    /// Изменить положение модели на карте (альтернативный маршрут: PUT /model/:model_id)
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "position": [x, y, z],
    ///   "rotation": [a, b, c],
    ///   "scale": number
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "id": "model_id",
    ///   "position": [lat, y, lng],
    ///   "rotation": [a, b, c],
    ///   "scale": 1.0
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено изменять
    /// </remarks>
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
    /// Получить модель с сервера
    /// </summary>
    /// <remarks>
    /// В ответ должен приходить бинарник модельки.
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
    /// Получить метаданные модели по ее адресу
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "address": "string"
    /// }
    /// ```
    ///
    /// **Ответ:**
    /// ```json
    /// {
    ///   "model_id": "string",
    ///   "address": "string",
    ///   "position": [x, 0, z],
    ///   "rotation": "угол в радианах",
    ///   "scale": "масштаб"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
    /// Сохранить метаданные модели
    /// </summary>
    /// <remarks>
    /// **Тело запроса:**
    /// ```json
    /// {
    ///   "position": [x, 0, z],
    ///   "rotation": "rotationAngle",
    ///   "scale": number,
    ///   "polygons": ["polygon1Id", "polygon2Id", ...],
    ///   "address": "string"
    /// }
    /// ```
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено сохранять
    /// </remarks>
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
    /// Получить модель с бэкенда по Id
    /// </summary>
    /// <remarks>
    /// В ответ должен приходить бинарник модельки.
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
    /// Получить модель с бэкенда по Id (альтернативный маршрут: GET /model/:model_id)
    /// </summary>
    /// <remarks>
    /// В ответ должен приходить бинарник модельки.
    ///
    /// **Ошибки:**
    /// - **404 Not Found**: Если модель не найдена
    /// - **401 Unauthorized**: Если не прилогинились
    /// - **403 Forbidden**: Если по роли не положено получать
    /// </remarks>
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
