using Microsoft.AspNetCore.Mvc;

namespace Excursion_GPT.Controllers;

[ApiController]
[Route("test")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Тестовый endpoint для проверки работы API
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// {
    ///   "message": "Test controller works!",
    ///   "timestamp": "2024-01-01T12:00:00Z"
    /// }
    /// ```
    ///
    /// Используется для проверки доступности API и базовой функциональности.
    /// </remarks>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Test controller works!", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Проверка здоровья сервиса
    /// </summary>
    /// <remarks>
    /// **Ответ:**
    /// ```json
    /// {
    ///   "status": "Healthy",
    ///   "service": "TestController",
    ///   "timestamp": "2024-01-01T12:00:00Z"
    /// }
    /// ```
    ///
    /// Используется для мониторинга здоровья сервиса. Возвращает статус "Healthy" если сервис работает нормально.
    /// </remarks>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "Healthy", service = "TestController", timestamp = DateTime.UtcNow });
    }
}
