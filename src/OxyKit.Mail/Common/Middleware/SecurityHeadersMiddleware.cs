namespace OxyKit.Mail.Common.Middleware;

/// <summary>
/// Middleware that adds security headers to all HTTP responses.
/// Implements OWASP-recommended headers to prevent common web vulnerabilities.
/// </summary>
/// <remarks>
/// Security headers added:
/// - X-Content-Type-Options: Prevents MIME sniffing attacks
/// - X-Frame-Options: Prevents clickjacking attacks
/// - X-XSS-Protection: Enables browser XSS protection
/// - Referrer-Policy: Controls referrer information leakage
/// - Permissions-Policy: Disables unnecessary browser features
/// - Content-Security-Policy: Prevents XSS and injection attacks
/// - Strict-Transport-Security: Enforces HTTPS (production only)
/// </remarks>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware to add security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Prevent MIME sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // Enable XSS protection (legacy browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable unnecessary browser features
        context.Response.Headers["Permissions-Policy"] =
            "geolocation=(), camera=(), microphone=(), payment=(), usb=(), magnetometer=()";

        // Content Security Policy (adjusted for Swagger/API usage)
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; "
            + "script-src 'self' 'unsafe-inline'; "
            + // Swagger needs unsafe-inline
            "style-src 'self' 'unsafe-inline'; "
            + "img-src 'self' data: https:; "
            + "font-src 'self'; "
            + "connect-src 'self'; "
            + "frame-ancestors 'none'; "
            + "base-uri 'self'; "
            + "form-action 'self'";

        // HSTS (Strict-Transport-Security) - only in production
        // Enforces HTTPS for 1 year, including subdomains
        if (!context.Request.Host.Host.Contains("localhost", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers["Strict-Transport-Security"] =
                "max-age=31536000; includeSubDomains; preload";
        }

        await next(context);
    }
}
