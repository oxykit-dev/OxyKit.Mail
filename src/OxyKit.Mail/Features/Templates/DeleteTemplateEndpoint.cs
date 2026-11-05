using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Templates;

public sealed class DeleteTemplateRequest
{
    public string TemplateId { get; set; } = "";
}

public sealed class DeleteTemplateEndpoint(IMailTemplateStore store)
    : MailEndpoint<DeleteTemplateRequest>
{
    public override void Configure()
    {
        Delete("/templates/{templateId}");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Summary(s =>
        {
            s.Summary = "Delete a mail template pack";
            s.Description = "Removes the template directory under MailTemplates:TemplatesRoot. Admin only.";
        });
    }

    public override async Task HandleAsync(DeleteTemplateRequest req, CancellationToken ct)
    {
        var result = await store.DeleteAsync(req.TemplateId, ct);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
