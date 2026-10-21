namespace OxyKit.Mail.Common.Middleware;

/// <summary>
/// Middleware that scans incoming JSON requests for dangerous patterns (XSS, injection attempts).
/// Blocks requests containing suspicious content with 400 Bad Request.
/// </summary>
/// <remarks>
/// This provides defense-in-depth alongside FastEndpoints validation and output encoding.
/// Dangerous patterns include: script tags, javascript: URLs, event handlers, eval, etc.
/// </remarks>
public class InputSanitizationMiddleware(
    RequestDelegate next,
    ILogger<InputSanitizationMiddleware> logger
)
{
    private static readonly string[] DangerousPatterns =
    [
        "<script",
        "</script>",
        "javascript:",
        "onerror=",
        "onload=",
        "onclick=",
        "onmouseover=",
        "eval(",
        "expression(",
        "<iframe",
        "<object",
        "<embed",
        "<applet",
        "vbscript:",
        "data:text/html",
    ];

    /// <summary>
    /// Invokes the middleware to scan request body for dangerous patterns.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Only scan JSON requests
        if (
            context.Request.ContentType?.Contains(
                "application/json",
                StringComparison.OrdinalIgnoreCase
            ) == true
        )
        {
            // Enable buffering so we can read the body multiple times
            context.Request.EnableBuffering();

            // Read the request body
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // Reset stream position for downstream middleware
            context.Request.Body.Position = 0;

            // Check for dangerous patterns (case-insensitive)
            foreach (var pattern in DangerousPatterns)
            {
                if (body.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    logger.LogWarning(
                        "Suspicious input detected from IP {IP}, Pattern: {Pattern}, Path: {Path}",
                        context.Connection.RemoteIpAddress,
                        pattern,
                        context.Request.Path
                    );

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "Invalid input",
                            message = "Request contains potentially dangerous content",
                        }
                    );

                    return;
                }
            }
        }

        await next(context);
    }
}
