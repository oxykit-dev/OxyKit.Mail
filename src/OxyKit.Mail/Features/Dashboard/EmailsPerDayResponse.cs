namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Response containing emails sent per day statistics.
/// </summary>
public record EmailsPerDayResponse(DateTime Date, int EmailsSent);
