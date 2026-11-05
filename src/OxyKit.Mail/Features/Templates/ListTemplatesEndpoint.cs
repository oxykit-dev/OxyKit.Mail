using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Templates;

public sealed class ListTemplatesEndpoint(IMailTemplateStore store)
    : MailEndpointWithoutRequest<MailTemplateListResponse>
{
    public override void Configure()
    {
        Get("/templates");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.Sender);
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Summary(s =>
        {
            s.Summary = "List mail template packs";
            s.Description =
                "Returns template ids under MailTemplates:TemplatesRoot (subject.txt + body.html).";
        });
    }

    public override Task HandleAsync(CancellationToken ct)
    {
        var items = store.List();
        return Send.OkAsync(new MailTemplateListResponse(items), cancellation: ct);
    }
}
