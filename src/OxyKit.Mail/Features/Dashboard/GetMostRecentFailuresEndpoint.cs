using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Endpoint for retrieving the most recent failed emails.
/// </summary>
public class GetMostRecentFailuresEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<List<MessageLogResponse>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/dashboard/most-recent-failures");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetMostRecentFailuresAsync(5, ct);
        await Send.OkAsync(result, ct);
    }
}
