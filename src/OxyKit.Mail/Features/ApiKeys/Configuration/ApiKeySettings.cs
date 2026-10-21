namespace OxyKit.Mail.Features.ApiKeys.Configuration;

/// <summary>
/// Configuration settings for API key security policies.
/// </summary>
public class ApiKeySettings
{
    /// <summary>
    /// Maximum age of an API key in days before it expires.
    /// Default: 90 days.
    /// </summary>
    public int MaxAgeInDays { get; set; } = 90;

    /// <summary>
    /// Number of days before expiry to start warning users.
    /// Default: 14 days.
    /// </summary>
    public int WarnBeforeExpiryDays { get; set; } = 14;

    /// <summary>
    /// Maximum number of days an API key can be inactive before being disabled.
    /// Default: 30 days.
    /// </summary>
    public int MaxInactiveTimeInDays { get; set; } = 30;
}
