using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Endpoint for retrieving dead-lettered emails.
/// </summary>
public class GetDeadLetteredEmailsEndpoint(IDashboardService dashboardService)
    : MailEndpointWithoutRequest<List<DeadLetteredEmailResponse>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/dashboard/dead-lettered-emails");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await dashboardService.GetDeadLetteredEmailsAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
