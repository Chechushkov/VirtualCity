using System.Net;
using System.Text.Json;
using Excursion_GPT.Domain.Common;

namespace Excursion_GPT.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception has occurred.");

        var statusCode = HttpStatusCode.InternalServerError;
        var errorObject = "server_error";
        var message = "An unexpected error occurred.";

        if (exception is BaseException customException)
        {
            statusCode = (HttpStatusCode)customException.StatusCode;
            errorObject = customException.Object;
            message = customException.Message;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var errorResponse = new
        {
            code = (int)statusCode,
            @object = errorObject,
            message = message
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}