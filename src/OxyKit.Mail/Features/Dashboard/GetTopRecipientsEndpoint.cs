using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

public class GetTopRecipientsEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<List<TopRecipientResponse>>
{
    public override void Configure()
    {
        Get("/dashboard/top-recipients");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetTopRecipientsAsync(10, ct);
        await Send.OkAsync(result, ct);
    }
}
