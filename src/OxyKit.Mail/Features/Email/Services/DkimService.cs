using ErrorOr;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using OxyKit.Mail.Features.Email.Configuration;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Service contract for DKIM (DomainKeys Identified Mail) email signing.
/// </summary>
public interface IDkimService
{
    ErrorOr<Success> SignMessage(MimeMessage message);
    ErrorOr<Success> ValidateConfiguration();
    bool IsEnabled();
    ErrorOr<(string PublicKey, string PrivateKey)> GenerateKeyPair(int keySize = 2048);
    ErrorOr<string> GetDnsTxtRecord();
}

/// <summary>
/// Service for DKIM (DomainKeys Identified Mail) email signing.
/// Signs outgoing emails with cryptographic signatures to prove authenticity.
/// </summary>
public class DkimService(
    IOptions<DkimSettings> dkimSettings,
    ILogger<DkimService> logger,
    IKeyStorage keyStorage
) : IDkimService
{
    private readonly DkimSettings _settings = dkimSettings.Value;
    private readonly Lazy<DkimSigner?> _signer = new(
        () => CreateSigner(dkimSettings.Value, logger, keyStorage),
        LazyThreadSafetyMode.ExecutionAndPublication
    );

    /// <summary>
    /// Signs an email message with DKIM signature.
    /// </summary>
    public ErrorOr<Success> SignMessage(MimeMessage message)
    {
        if (!_settings.Enabled)
        {
            logger.LogDebug("DKIM signing is disabled, skipping");
            return Result.Success;
        }

        try
        {
            var signer = _signer.Value;
            if (signer == null)
            {
                return Error.Failure(
                    code: "Dkim.SignerNotInitialized",
                    description: "DKIM signer could not be initialized"
                );
            }

            message.Prepare(EncodingConstraint.SevenBit);
            signer.Sign(message, _settings.HeadersToSign);

            logger.LogInformation(
                "DKIM signed email with domain={Domain}, selector={Selector}",
                _settings.Domain,
                _settings.Selector
            );

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sign message with DKIM");
            return Error.Failure(
                code: "Dkim.SigningFailed",
                description: $"DKIM signing failed: {ex.Message}"
            );
        }
    }

    public ErrorOr<Success> ValidateConfiguration()
    {
        if (!_settings.Enabled)
            return Result.Success;

        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(_settings.Domain))
            errors.Add(
                Error.Validation(
                    code: "Dkim.DomainRequired",
                    description: "DKIM domain is required when DKIM is enabled"
                )
            );

        if (string.IsNullOrWhiteSpace(_settings.Selector))
            errors.Add(
                Error.Validation(
                    code: "Dkim.SelectorRequired",
                    description: "DKIM selector is required when DKIM is enabled"
                )
            );

        if (string.IsNullOrWhiteSpace(_settings.PrivateKeyPath))
            errors.Add(
                Error.Validation(
                    code: "Dkim.PrivateKeyRequired",
                    description: "DKIM private key path is required when DKIM is enabled"
                )
            );
        else if (!keyStorage.ExistsAsync(_settings.PrivateKeyPath).GetAwaiter().GetResult())
            errors.Add(
                Error.Validation(
                    code: "Dkim.PrivateKeyNotFound",
                    description: $"DKIM private key file not found: {_settings.PrivateKeyPath}"
                )
            );
        else
        {
            try
            {
                LoadPrivateKey(keyStorage, _settings.PrivateKeyPath);
            }
            catch (Exception ex)
            {
                errors.Add(
                    Error.Validation(
                        code: "Dkim.PrivateKeyInvalid",
                        description: $"DKIM private key is invalid: {ex.Message}"
                    )
                );
            }
        }

        if (errors.Count > 0)
            return errors;

        logger.LogInformation("DKIM configuration is valid");
        return Result.Success;
    }

    public bool IsEnabled() => _settings.Enabled;

    public ErrorOr<(string PublicKey, string PrivateKey)> GenerateKeyPair(int keySize = 2048)
    {
        if (keySize != 1024 && keySize != 2048 && keySize != 4096)
            return Error.Validation(
                code: "Dkim.InvalidKeySize",
                description: "Key size must be 1024, 2048, or 4096 bits"
            );

        try
        {
            var keyGenerator = new RsaKeyPairGenerator();
            keyGenerator.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
            var keyPair = keyGenerator.GenerateKeyPair();

            string privateKeyPem;
            using (var stringWriter = new StringWriter())
            {
                var pemWriter = new PemWriter(stringWriter);
                pemWriter.WriteObject(keyPair.Private);
                privateKeyPem = stringWriter.ToString();
            }

            var publicKey = (RsaKeyParameters)keyPair.Public;
            var modulus = Convert.ToBase64String(publicKey.Modulus.ToByteArrayUnsigned());
            var publicKeyDns = $"v=DKIM1; k=rsa; p={modulus}";

            logger.LogInformation("Generated DKIM key pair with {KeySize} bits", keySize);
            return (publicKeyDns, privateKeyPem);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate DKIM key pair");
            return Error.Failure(
                code: "Dkim.KeyGenerationFailed",
                description: $"Failed to generate DKIM key pair: {ex.Message}"
            );
        }
    }

    public ErrorOr<string> GetDnsTxtRecord()
    {
        if (!_settings.Enabled)
            return Error.Failure(code: "Dkim.NotEnabled", description: "DKIM is not enabled");

        if (!keyStorage.ExistsAsync(_settings.PrivateKeyPath).GetAwaiter().GetResult())
            return Error.Failure(
                code: "Dkim.PrivateKeyNotFound",
                description: "Private key file not found"
            );

        try
        {
            var privateKey = LoadPrivateKey(keyStorage, _settings.PrivateKeyPath);
            var publicKey = DerivePublicKey(privateKey);
            var modulus = Convert.ToBase64String(publicKey.Modulus.ToByteArrayUnsigned());
            var dnsRecord = $"v=DKIM1; k=rsa; p={modulus}";

            logger.LogInformation("Generated DNS TXT record for DKIM");
            return dnsRecord;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate DNS TXT record");
            return Error.Failure(
                code: "Dkim.DnsTxtFailed",
                description: $"Failed to generate DNS TXT record: {ex.Message}"
            );
        }
    }

    private static DkimSigner? CreateSigner(
        DkimSettings settings,
        ILogger<DkimService> logger,
        IKeyStorage keyStorage
    )
    {
        try
        {
            LoadPrivateKey(keyStorage, settings.PrivateKeyPath);
            var signer = new DkimSigner(settings.PrivateKeyPath, settings.Domain, settings.Selector)
            {
                HeaderCanonicalizationAlgorithm = ParseCanonicalization(
                    settings.HeaderCanonicalization
                ),
                BodyCanonicalizationAlgorithm = ParseCanonicalization(
                    settings.BodyCanonicalization
                ),
                SignatureAlgorithm = ParseSignatureAlgorithm(settings.SignatureAlgorithm),
            };

            logger.LogInformation("DKIM signer initialized successfully");
            return signer;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize DKIM signer");
            return null;
        }
    }

    private static RsaPrivateCrtKeyParameters LoadPrivateKey(IKeyStorage keyStorage, string keyPath)
    {
        var keyResult = keyStorage.ReadKeyAsync(keyPath).GetAwaiter().GetResult();
        if (keyResult.IsError)
            throw new InvalidOperationException(
                $"Failed to read private key: {keyResult.FirstError.Description}"
            );

        using var reader = new StringReader(keyResult.Value);
        var pemReader = new PemReader(reader);
        var keyObject = pemReader.ReadObject();

        return keyObject switch
        {
            RsaPrivateCrtKeyParameters rsaKey => rsaKey,
            AsymmetricCipherKeyPair keyPair => (RsaPrivateCrtKeyParameters)keyPair.Private,
            _ => throw new InvalidOperationException("Invalid private key format"),
        };
    }

    private static RsaKeyParameters DerivePublicKey(RsaPrivateCrtKeyParameters privateKey) =>
        new(false, privateKey.Modulus, privateKey.PublicExponent);

    private static DkimCanonicalizationAlgorithm ParseCanonicalization(string value) =>
        value.ToLowerInvariant() switch
        {
            "simple" => DkimCanonicalizationAlgorithm.Simple,
            "relaxed" => DkimCanonicalizationAlgorithm.Relaxed,
            _ => DkimCanonicalizationAlgorithm.Relaxed,
        };

    private static DkimSignatureAlgorithm ParseSignatureAlgorithm(string value) =>
        value.ToLowerInvariant() switch
        {
            "rsa-sha1" => DkimSignatureAlgorithm.RsaSha1,
            "rsa-sha256" => DkimSignatureAlgorithm.RsaSha256,
            _ => DkimSignatureAlgorithm.RsaSha256,
        };
}
