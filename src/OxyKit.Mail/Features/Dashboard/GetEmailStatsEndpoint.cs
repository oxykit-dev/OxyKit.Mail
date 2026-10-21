using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Endpoint for retrieving email statistics.
/// </summary>
public class GetEmailStatsEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<EmailStatsResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/dashboard/email-stats");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetEmailStatsAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
