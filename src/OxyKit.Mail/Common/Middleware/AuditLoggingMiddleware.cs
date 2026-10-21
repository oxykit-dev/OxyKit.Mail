namespace OxyKit.Mail.Common.Middleware;

/// <summary>
/// Middleware that logs security-sensitive events for audit trails and forensics.
/// Tracks authentication attempts, authorization failures, and access to sensitive endpoints.
/// </summary>
public class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    private static readonly string[] SensitiveEndpoints =
    [
        "/auth/login",
        "/auth/register",
        "/auth/refresh",
        "/apikeys",
        "/users/create-admin",
        "/setup/create-admin",
    ];

    /// <summary>
    /// Invokes the middleware to log security events.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var user = context.User.Identity?.Name ?? "Anonymous";

        // Log security-sensitive requests
        if (
            SensitiveEndpoints.Any(endpoint =>
                path.Contains(endpoint, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            logger.LogInformation(
                "Security Event: {Method} {Path} from {IP}, User: {User}",
                method,
                path,
                ip,
                user
            );
        }

        // Call next middleware
        await next(context);

        // Log failed authentication attempts (401 Unauthorized)
        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            logger.LogWarning(
                "Failed authentication: {Method} {Path} from {IP}, User: {User}",
                method,
                path,
                ip,
                user
            );
        }

        // Log authorization failures (403 Forbidden)
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            logger.LogWarning(
                "Authorization denied: {Method} {Path}, User: {User} from {IP}",
                method,
                path,
                user,
                ip
            );
        }

        // Log rate limit violations (429 Too Many Requests)
        if (context.Response.StatusCode == StatusCodes.Status429TooManyRequests)
        {
            logger.LogWarning(
                "Rate limit exceeded: {Method} {Path} from {IP}, User: {User}",
                method,
                path,
                ip,
                user
            );
        }
    }
}
