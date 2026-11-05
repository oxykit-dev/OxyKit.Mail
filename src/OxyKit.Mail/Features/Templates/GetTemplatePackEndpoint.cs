using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Templates;

public sealed class GetTemplatePackRequest
{
    public string TemplateId { get; set; } = "";
}

public sealed class GetTemplatePackEndpoint(IMailTemplateStore store)
    : MailEndpoint<GetTemplatePackRequest>
{
    public override void Configure()
    {
        Get("/templates/{templateId}/pack");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.Sender);
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Summary(s =>
        {
            s.Summary = "Download mail template pack zip";
            s.Description = "Zips subject.txt, body.html, and optional contract.json / sample-data.json.";
        });
    }

    public override async Task HandleAsync(GetTemplatePackRequest req, CancellationToken ct)
    {
        var result = await store.ExportZipAsync(req.TemplateId, ct);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.BytesAsync(
            result.Value,
            fileName: $"{req.TemplateId}.zip",
            contentType: "application/zip",
            cancellation: ct
        );
    }
}
