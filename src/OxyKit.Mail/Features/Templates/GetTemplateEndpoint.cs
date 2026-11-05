using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Templates;

public sealed class GetTemplateRequest
{
    public string TemplateId { get; set; } = "";
}

public sealed class GetTemplateEndpoint(IMailTemplateStore store)
    : MailEndpoint<GetTemplateRequest, MailTemplateDetailResponse>
{
    public override void Configure()
    {
        Get("/templates/{templateId}");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.Sender);
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Summary(s =>
        {
            s.Summary = "Get mail template pack metadata";
            s.Description = "Returns relative file list for one pack. Use /templates/{id}/pack for zip download.";
        });
    }

    public override async Task HandleAsync(GetTemplateRequest req, CancellationToken ct)
    {
        var result = store.Describe(req.TemplateId);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.OkAsync(
            new MailTemplateDetailResponse(result.Value.Id, result.Value.Files),
            cancellation: ct
        );
    }
}
