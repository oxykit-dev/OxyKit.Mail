namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// OAuth2 grant used to obtain SMTP access tokens.
/// </summary>
public enum SmtpOAuth2GrantType
{
    /// <summary>
    /// Shared default for Microsoft and Google: refresh a long-lived refresh token.
    /// </summary>
    RefreshToken = 0,

    /// <summary>
    /// Microsoft 365 app-only (client credentials). Not supported for Google SMTP.
    /// </summary>
    ClientCredentials = 1,
}
