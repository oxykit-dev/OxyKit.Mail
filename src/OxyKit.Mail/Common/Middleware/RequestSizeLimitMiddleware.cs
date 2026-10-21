using Microsoft.Extensions.Options;
using OxyKit.Mail.Common.Configuration;

namespace OxyKit.Mail.Common.Middleware;

/// <summary>
/// Middleware that enforces HTTP request size limits to prevent DoS attacks.
/// Rejects requests exceeding configured size limits with 413 Payload Too Large.
/// </summary>
public class RequestSizeLimitMiddleware(
    RequestDelegate next,
    IOptions<RequestLimitsSettings> settings,
    ILogger<RequestSizeLimitMiddleware> logger
)
{
    private readonly RequestLimitsSettings _settings = settings.Value;

    /// <summary>
    /// Invokes the middleware to check request size before processing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only check POST, PUT, PATCH requests (methods with body)
        if (context.Request.Method is "POST" or "PUT" or "PATCH")
        {
            // Check if Content-Length header exists
            if (context.Request.ContentLength.HasValue)
            {
                var contentLength = context.Request.ContentLength.Value;

                // Reject if exceeds maximum
                if (contentLength > _settings.MaxRequestBodySize)
                {
                    logger.LogWarning(
                        "Request size limit exceeded: {ContentLength} bytes from IP {IP}, Path: {Path}",
                        contentLength,
                        context.Connection.RemoteIpAddress,
                        context.Request.Path
                    );

                    context.Response.StatusCode = StatusCodes.Status413PayloadTooLarge;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "Payload too large",
                            message = $"Request size exceeds maximum allowed ({_settings.MaxRequestBodySize:N0} bytes)",
                            maxSize = _settings.MaxRequestBodySize,
                            actualSize = contentLength,
                        }
                    );
                    return;
                }
            }
        }

        await next(context);
    }
}
