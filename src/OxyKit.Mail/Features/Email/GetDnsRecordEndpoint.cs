using Microsoft.Extensions.Options;
using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Gets the DNS TXT record for DKIM configuration.
/// </summary>
public class GetDnsRecordEndpoint(IDkimService dkimService, IOptions<DkimSettings> dkimSettings)
    : MailEndpointWithoutRequest<GetDnsRecordResponse>
{
    private readonly DkimSettings _settings = dkimSettings.Value;

    /// <inheritdoc />
    public override void Configure()
    {
        Get("/email/dkim/dns-record");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Summary(s =>
        {
            s.Summary = "Get DKIM DNS TXT record";
            s.Description =
                "Returns the DNS TXT record that should be published for DKIM verification.";
            s.ExampleRequest = new { };
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!dkimService.IsEnabled())
        {
            AddError("DKIM is not enabled in configuration");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var result = dkimService.GetDnsTxtRecord();
        if (await SendOnErrorAsync(result, ct))
            return;

        var txtRecord = result.Value;
        var instructions = GetInstructions(_settings.Selector, _settings.Domain, txtRecord);

        await Send.OkAsync(
            new GetDnsRecordResponse(
                true,
                txtRecord,
                instructions,
                "DNS TXT record retrieved successfully"
            ),
            ct
        );
    }

    private static string GetInstructions(string selector, string domain, string txtRecord) =>
        $"""
            Add this TXT record to your DNS:

            Host/Name: {selector}._domainkey.{domain}
            Type: TXT
            Value: {txtRecord}
            TTL: 3600

            Example for Cloudflare:
            - Type: TXT
            - Name: {selector}._domainkey
            - Content: {txtRecord}
            - TTL: Auto

            Example for AWS Route53:
            - Name: {selector}._domainkey.{domain}.
            - Type: TXT
            - Value: "{txtRecord}"
            - TTL: 3600

            After adding the record, wait for DNS propagation (5-60 minutes).

            Verify with:
            dig {selector}._domainkey.{domain} TXT

            or

            nslookup -type=TXT {selector}._domainkey.{domain}
            """;
}
