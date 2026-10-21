using System.Net;
using System.Text.Json;

namespace OxyKit.Mail.Common.Middleware;

/// <summary>
/// Catches unhandled exceptions outside FastEndpoints and returns the same
/// shape FE uses for <c>Send.ErrorsAsync</c> (<c>statusCode</c> + <c>message</c> + <c>errors</c>).
/// </summary>
public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger
)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception occurred.");
            if (context.Response.HasStarted)
                throw;

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var body = new
            {
                statusCode = 500,
                message = "An unexpected error occurred.",
                errors = new Dictionary<string, string[]>(),
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(body, JsonOptions));
        }
    }
}
