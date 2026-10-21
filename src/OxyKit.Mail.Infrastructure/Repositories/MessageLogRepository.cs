using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Repository for managing message log persistence and retrieval.
/// </summary>
public class MessageLogRepository(ApplicationDbContext context) : IMessageLogRepository
{
    public async Task AddAsync(MessageLog log, CancellationToken ct)
    {
        context.MessageLogs.Add(log);
        await context.SaveChangesAsync(ct);
    }

    public async Task<List<MessageLog>> GetByMessageIdAsync(Guid messageId, CancellationToken ct)
    {
        return await context.MessageLogs.Where(l => l.MessageId == messageId).ToListAsync(ct);
    }

    public async Task<List<MessageLog>> GetRecentAsync(int count, CancellationToken ct)
    {
        return await context
            .MessageLogs.OrderByDescending(l => l.LoggedAt)
            .Take(count)
            .Include(l => l.Message)
            .ToListAsync(ct);
    }

    public async Task<List<MessageLog>> GetRecentFailuresAsync(int count, CancellationToken ct)
    {
        return await context
            .MessageLogs.Where(l => l.Status == MessageStatus.Failed)
            .OrderByDescending(l => l.LoggedAt)
            .Take(count)
            .Include(l => l.Message)
            .ToListAsync(ct);
    }

    public async Task<List<(DateTime Date, int EmailsSent)>> GetEmailsPerDayAsync(
        DateTime since,
        string timeZoneId,
        CancellationToken ct
    )
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var sinceUtc = DateTime.SpecifyKind(since, DateTimeKind.Utc);
        var todayUtc = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var sinceLocal = TimeZoneInfo.ConvertTimeFromUtc(sinceUtc, tz).Date;
        var todayLocal = TimeZoneInfo.ConvertTimeFromUtc(todayUtc, tz).Date;
        var allDates = Enumerable
            .Range(0, (todayLocal - sinceLocal).Days + 1)
            .Select(offset => sinceLocal.AddDays(offset))
            .ToList();

        var sentLogs = await context
            .MessageLogs.Where(e => e.Status == MessageStatus.Sent && e.LoggedAt >= sinceUtc)
            .ToListAsync(ct);

        var grouped = sentLogs
            .GroupBy(e =>
                TimeZoneInfo
                    .ConvertTimeFromUtc(DateTime.SpecifyKind(e.LoggedAt, DateTimeKind.Utc), tz)
                    .Date
            )
            .ToDictionary(g => g.Key, g => g.Count());

        return allDates.ConvertAll(date =>
            (date, grouped.TryGetValue(date, out var count) ? count : 0)
        );
    }

    public async Task<(int totalSent, int totalFailed)> GetDeliveryStatsAsync(CancellationToken ct)
    {
        var totalSent = await context.MessageLogs.CountAsync(
            e => e.Status == MessageStatus.Sent,
            ct
        );
        var totalFailed = await context.MessageLogs.CountAsync(
            e => e.Status == MessageStatus.Failed,
            ct
        );
        return (totalSent, totalFailed);
    }

    public async Task<DateTime> GetLastEmailSentAtAsync(CancellationToken ct)
    {
        return await context
            .MessageLogs.Where(e => e.Status == MessageStatus.Sent)
            .OrderByDescending(e => e.LoggedAt)
            .Select(e => e.LoggedAt)
            .FirstOrDefaultAsync(ct);
    }
}
