using System.Text.Json;
using webapi.Exceptions;

namespace webapi.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IHostEnvironment env
    )
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");

            var response = context.Response;
            response.ContentType = "application/json";

            var statusCode = ex switch
            {
                AppException => StatusCodes.Status400BadRequest,
                NotFoundException => StatusCodes.Status404NotFound,
                _ => StatusCodes.Status500InternalServerError,
            };
            response.StatusCode = statusCode;

            var errorResponse = new
            {
                Success = false,
                Error = ex.Message,
                Errors = _env.IsDevelopment() ? new[] { ex.StackTrace ?? "" } : null,
            };

            await response.WriteAsync(JsonSerializer.Serialize(errorResponse));
        }
    }
}
