using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Endpoint for retrieving emails per day statistics.
/// </summary>
public class GetEmailsPerDayEndpoint(IDashboardService dashboardService)
    : MailEndpoint<EmailsPerDayRequest, List<EmailsPerDayResponse>>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/dashboard/emails-per-day");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    /// <inheritdoc />
    public override async Task HandleAsync(EmailsPerDayRequest req, CancellationToken ct)
    {
        var since = req.Since ?? DateTime.UtcNow.AddDays(-7);
        var timeZoneId = req.TimeZoneId ?? "UTC";
        var result = await dashboardService.GetEmailsPerDayAsync(since, timeZoneId, ct);
        await Send.OkAsync(result, ct);
    }
}
