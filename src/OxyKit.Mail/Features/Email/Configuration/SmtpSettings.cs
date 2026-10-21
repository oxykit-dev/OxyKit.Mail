using System.ComponentModel.DataAnnotations;

namespace OxyKit.Mail.Features.Email.Configuration;

public record SmtpSettings
{
    [Required]
    public string Host { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; }

    public bool EnableSsl { get; init; }

    /// <summary>
    /// SMTP mailbox / From address. For OAuth2 this must be the mailbox the app can send as.
    /// </summary>
    [Required]
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// SMTP password. Required when <see cref="AuthMode"/> is <see cref="SmtpAuthMode.Password"/>.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Authentication mode. Defaults to password for backward compatibility.
    /// </summary>
    public SmtpAuthMode AuthMode { get; init; } = SmtpAuthMode.Password;

    /// <summary>
    /// OAuth2 settings. Required when <see cref="AuthMode"/> is <see cref="SmtpAuthMode.OAuth2"/>.
    /// </summary>
    public SmtpOAuth2Settings? OAuth2 { get; init; }
}
