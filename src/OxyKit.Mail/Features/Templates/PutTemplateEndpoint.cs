using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Templates;

public sealed class PutTemplateRequest
{
    public string TemplateId { get; set; } = "";
}

public sealed class PutTemplateEndpoint(IMailTemplateStore store)
    : MailEndpoint<PutTemplateRequest, MailTemplateInfo>
{
    public override void Configure()
    {
        Put("/templates/{templateId}");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Summary(s =>
        {
            s.Summary = "Upload or replace a mail template pack";
            s.Description =
                "Accepts a zip (application/zip) with subject.txt + body.html at the root "
                + "(or one top-level folder). Optional contract.json / sample-data.json. Admin only.";
        });
    }

    public override async Task HandleAsync(PutTemplateRequest req, CancellationToken ct)
    {
        var contentType = HttpContext.Request.ContentType ?? "";
        if (
            !contentType.Contains("application/zip", StringComparison.OrdinalIgnoreCase)
            && !contentType.Contains("application/x-zip-compressed", StringComparison.OrdinalIgnoreCase)
            && !contentType.Contains("application/octet-stream", StringComparison.OrdinalIgnoreCase)
        )
        {
            AddError("Content-Type must be application/zip (or application/octet-stream).");
            await Send.ErrorsAsync(statusCode: 400, cancellation: ct);
            return;
        }

        var result = await store.SaveFromZipAsync(req.TemplateId, HttpContext.Request.Body, ct);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.OkAsync(result.Value, cancellation: ct);
    }
}
