namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// How OxyKit.Mail authenticates to the outbound SMTP server.
/// </summary>
public enum SmtpAuthMode
{
    /// <summary>Classic SMTP username + password (LOGIN/PLAIN).</summary>
    Password = 0,

    /// <summary>OAuth2 access token via SASL XOAUTH2 (e.g. Microsoft 365).</summary>
    OAuth2 = 1,
}
