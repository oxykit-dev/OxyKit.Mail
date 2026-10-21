using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Generates a new DKIM key pair for email signing.
/// </summary>
public class GenerateKeysEndpoint(IDkimService dkimService)
    : MailEndpointWithoutRequest<GenerateKeysResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/email/dkim/generate-keys");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Summary(s =>
        {
            s.Summary = "Generate DKIM key pair";
            s.Description =
                "Generates a new RSA key pair for DKIM email signing. Returns public key for DNS and private key for server.";
            s.ExampleRequest = new { };
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = dkimService.GenerateKeyPair(2048);
        if (await SendOnErrorAsync(result, ct))
            return;

        var (publicKey, privateKey) = result.Value;

        await Send.OkAsync(
            new GenerateKeysResponse(
                true,
                publicKey,
                privateKey,
                "DKIM keys generated successfully. Save the private key securely!"
            ),
            ct
        );
    }
}
