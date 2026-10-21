namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Provides OAuth2 access tokens for SMTP XOAUTH2 authentication.
/// </summary>
public interface ISmtpAccessTokenProvider
{
    /// <summary>
    /// Returns a usable access token, refreshing from the identity provider when needed.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}
