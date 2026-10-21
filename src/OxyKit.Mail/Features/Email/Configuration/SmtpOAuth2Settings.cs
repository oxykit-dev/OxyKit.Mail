namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// Config-only OAuth2 settings for SMTP XOAUTH2 (Microsoft 365 and Google).
/// Preferred grant for both providers is <see cref="SmtpOAuth2GrantType.RefreshToken"/>.
/// </summary>
public record SmtpOAuth2Settings
{
    /// <summary>Identity provider. Defaults to Microsoft.</summary>
    public SmtpOAuth2Provider Provider { get; init; } = SmtpOAuth2Provider.Microsoft;

    /// <summary>
    /// Token grant. Defaults to refresh token (shared workflow for M365 and Google).
    /// </summary>
    public SmtpOAuth2GrantType GrantType { get; init; } = SmtpOAuth2GrantType.RefreshToken;

    /// <summary>OAuth client / app ID.</summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>OAuth client secret.</summary>
    public string ClientSecret { get; init; } = string.Empty;

    /// <summary>
    /// Refresh token from a one-time consent. Required when
    /// <see cref="GrantType"/> is <see cref="SmtpOAuth2GrantType.RefreshToken"/>.
    /// </summary>
    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>
    /// Entra tenant ID. Required for Microsoft; ignored for Google.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>
    /// OAuth scope. Empty uses the provider/grant default
    /// (see <see cref="SmtpOAuth2Defaults"/>).
    /// </summary>
    public string Scope { get; init; } = string.Empty;

    /// <summary>
    /// Entra authority host (no trailing slash). Microsoft only.
    /// </summary>
    public string Authority { get; init; } = "https://login.microsoftonline.com";

    /// <summary>
    /// Absolute token endpoint URL. Empty derives the provider default.
    /// </summary>
    public string TokenEndpoint { get; init; } = string.Empty;
}
