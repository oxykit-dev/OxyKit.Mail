namespace OxyKit.Mail.Common.Configuration;

/// <summary>
/// Rate limiting configuration settings.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Authentication endpoint rate limit settings.
    /// </summary>
    public AuthRateLimitSettings Auth { get; set; } = new();

    /// <summary>
    /// Email sending endpoint rate limit settings.
    /// </summary>
    public EmailRateLimitSettings Email { get; set; } = new();

    /// <summary>
    /// API endpoint rate limit settings.
    /// </summary>
    public ApiRateLimitSettings Api { get; set; } = new();
}

/// <summary>
/// Rate limit settings for authentication endpoints.
/// </summary>
public class AuthRateLimitSettings
{
    /// <summary>
    /// Maximum number of authentication requests allowed per window.
    /// Default: 10 requests.
    /// </summary>
    public int PermitLimit { get; set; } = 10;

    /// <summary>
    /// Time window in seconds for the rate limit.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of queued requests.
    /// Default: 5.
    /// </summary>
    public int QueueLimit { get; set; } = 5;
}

/// <summary>
/// Rate limit settings for email sending endpoints.
/// </summary>
public class EmailRateLimitSettings
{
    /// <summary>
    /// Maximum number of email requests allowed per window.
    /// Default: 50 requests (prevents spam while allowing legitimate bulk sending).
    /// </summary>
    public int PermitLimit { get; set; } = 50;

    /// <summary>
    /// Time window in seconds for the rate limit.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of queued requests.
    /// Default: 10.
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}

/// <summary>
/// Rate limit settings for general API endpoints.
/// </summary>
public class ApiRateLimitSettings
{
    /// <summary>
    /// Maximum number of API requests allowed per window.
    /// Default: 100 requests.
    /// </summary>
    public int PermitLimit { get; set; } = 100;

    /// <summary>
    /// Time window in seconds for the rate limit.
    /// Default: 60 seconds (1 minute).
    /// </summary>
    public int WindowSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of queued requests.
    /// Default: 10.
    /// </summary>
    public int QueueLimit { get; set; } = 10;
}
