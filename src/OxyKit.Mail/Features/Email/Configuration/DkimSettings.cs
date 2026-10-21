namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// Configuration settings for DKIM (DomainKeys Identified Mail) email signing.
/// Conditional required fields are enforced by <see cref="DkimSettingsValidator"/>.
/// </summary>
public class DkimSettings
{
    /// <summary>
    /// Whether DKIM signing is enabled.
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// The domain name used for DKIM signing (e.g., "example.com").
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// The DKIM selector (e.g., "default", "2024-01").
    /// </summary>
    public string Selector { get; init; } = "default";

    /// <summary>
    /// Path to the private key file (PEM format) used for signing.
    /// </summary>
    public string PrivateKeyPath { get; init; } = string.Empty;

    /// <summary>
    /// Headers to include in the DKIM signature.
    /// </summary>
    public string[] HeadersToSign { get; init; } = ["from", "to", "subject", "date", "message-id"];

    /// <summary>
    /// DKIM canonicalization algorithm for headers (<c>simple</c> or <c>relaxed</c>).
    /// </summary>
    public string HeaderCanonicalization { get; init; } = "relaxed";

    /// <summary>
    /// DKIM canonicalization algorithm for body (<c>simple</c> or <c>relaxed</c>).
    /// </summary>
    public string BodyCanonicalization { get; init; } = "relaxed";

    /// <summary>
    /// DKIM signature algorithm (<c>rsa-sha256</c> recommended).
    /// </summary>
    public string SignatureAlgorithm { get; init; } = "rsa-sha256";

    /// <summary>
    /// Whether to validate the configuration on startup.
    /// </summary>
    public bool ValidateOnStartup { get; init; } = true;
}
