using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Repository contract for MessageLog entity operations.
/// </summary>
public interface IMessageLogRepository
{
    /// <summary>
    /// Adds a new message log entry.
    /// </summary>
    Task AddAsync(MessageLog log, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent message logs.
    /// </summary>
    Task<List<MessageLog>> GetRecentAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Gets the most recent failed message logs.
    /// </summary>
    Task<List<MessageLog>> GetRecentFailuresAsync(int count, CancellationToken ct = default);

    /// <summary>
    /// Gets delivery statistics (sent and failed counts).
    /// </summary>
    Task<(int totalSent, int totalFailed)> GetDeliveryStatsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the timestamp of the last sent email.
    /// </summary>
    Task<DateTime> GetLastEmailSentAtAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets emails grouped by day for a specific time zone.
    /// </summary>
    Task<List<(DateTime Date, int EmailsSent)>> GetEmailsPerDayAsync(
        DateTime since,
        string timeZoneId,
        CancellationToken ct = default
    );
}
