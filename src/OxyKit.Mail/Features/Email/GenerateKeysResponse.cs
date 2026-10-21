namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Response containing generated DKIM keys.
/// </summary>
/// <param name="Success">Whether key generation succeeded.</param>
/// <param name="PublicKey">Public key in DNS TXT record format.</param>
/// <param name="PrivateKey">Private key in PEM format (keep secure!).</param>
/// <param name="Message">Success or error message.</param>
public record GenerateKeysResponse(
    bool Success,
    string PublicKey,
    string PrivateKey,
    string Message
);
