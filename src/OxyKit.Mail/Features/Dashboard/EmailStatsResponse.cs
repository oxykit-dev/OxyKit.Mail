namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Response containing overall email statistics.
/// </summary>
public record EmailStatsResponse(
    int TotalEmailsSent,
    int TotalEmailsFailed,
    int TotalEmailsQueued,
    DateTime LastEmailSentAt
);
