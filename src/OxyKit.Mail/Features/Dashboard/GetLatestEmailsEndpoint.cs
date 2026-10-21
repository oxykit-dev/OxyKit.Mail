using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Endpoint for retrieving the latest emails.
/// </summary>
public class GetLatestEmailsEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<List<MessageLogResponse>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/dashboard/latest-emails");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetLatestEmailsAsync(10, ct);
        await Send.OkAsync(result, ct);
    }
}
