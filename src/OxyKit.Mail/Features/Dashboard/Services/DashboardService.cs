using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Features.Dashboard.Services;

/// <summary>
/// Dashboard statistics and reporting (always succeeds; empty data returns zeros/empty lists).
/// </summary>
public interface IDashboardService
{
    Task<List<MessageLogResponse>> GetLatestEmailsAsync(int count, CancellationToken ct);

    Task<List<MessageLogResponse>> GetMostRecentFailuresAsync(int count, CancellationToken ct);

    Task<List<EmailsPerDayResponse>> GetEmailsPerDayAsync(
        DateTime since,
        string timeZoneId,
        CancellationToken ct
    );

    Task<DeliverySuccessRateResponse> GetDeliverySuccessRateAsync(CancellationToken ct);

    Task<EmailStatsResponse> GetEmailStatsAsync(CancellationToken ct);

    Task<List<TopRecipientResponse>> GetTopRecipientsAsync(int count, CancellationToken ct);

    Task<List<DeadLetteredEmailResponse>> GetDeadLetteredEmailsAsync(CancellationToken ct);
}

public class DashboardService(
    IMessageLogRepository messageLogRepository,
    IMessageRepository messageRepository
) : IDashboardService
{
    public async Task<List<MessageLogResponse>> GetLatestEmailsAsync(
        int count,
        CancellationToken ct
    )
    {
        var logs = await messageLogRepository.GetRecentAsync(count, ct);
        return logs?.ConvertAll(DashboardMappers.ToMessageLogResponse) ?? [];
    }

    public async Task<List<MessageLogResponse>> GetMostRecentFailuresAsync(
        int count,
        CancellationToken ct
    )
    {
        var logs = await messageLogRepository.GetRecentFailuresAsync(count, ct);
        return logs?.ConvertAll(DashboardMappers.ToMessageLogResponse) ?? [];
    }

    public async Task<List<EmailsPerDayResponse>> GetEmailsPerDayAsync(
        DateTime since,
        string timeZoneId,
        CancellationToken ct
    )
    {
        var grouped = await messageLogRepository.GetEmailsPerDayAsync(since, timeZoneId, ct);
        return grouped?.ConvertAll(g => new EmailsPerDayResponse(g.Date, g.EmailsSent)) ?? [];
    }

    public async Task<DeliverySuccessRateResponse> GetDeliverySuccessRateAsync(CancellationToken ct)
    {
        var (totalSent, totalFailed) = await messageLogRepository.GetDeliveryStatsAsync(ct);
        var successRate =
            totalSent + totalFailed > 0 ? (double)totalSent / (totalSent + totalFailed) * 100 : 0;
        return new DeliverySuccessRateResponse(successRate, totalSent, totalFailed);
    }

    public async Task<EmailStatsResponse> GetEmailStatsAsync(CancellationToken ct)
    {
        var totalEmailsSent = await messageLogRepository.GetDeliveryStatsAsync(ct);
        var totalEmailsQueued = await messageRepository.GetQueuedCountAsync(ct);
        var lastEmailSentAt = await messageLogRepository.GetLastEmailSentAtAsync(ct);
        var (sent, failed) = totalEmailsSent;
        return new EmailStatsResponse(sent, failed, totalEmailsQueued, lastEmailSentAt);
    }

    public async Task<List<TopRecipientResponse>> GetTopRecipientsAsync(
        int count,
        CancellationToken ct
    )
    {
        var emailMessages = await messageRepository.GetSentToAddressesAsync(ct);
        return emailMessages
                ?.SelectMany(to => to.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .GroupBy(r => r.Trim())
                .Select(g => new TopRecipientResponse(g.Key, g.Count()))
                .OrderByDescending(r => r.EmailsReceived)
                .Take(count)
                .ToList()
            ?? [];
    }

    public async Task<List<DeadLetteredEmailResponse>> GetDeadLetteredEmailsAsync(
        CancellationToken ct
    )
    {
        var messages = await messageRepository.GetDeadLetteredMessagesAsync(ct);
        return messages?.ConvertAll(DashboardMappers.ToDeadLetteredEmailResponse) ?? [];
    }
}
