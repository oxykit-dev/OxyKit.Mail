using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Repository for managing email message persistence and retrieval.
/// </summary>
public class MessageRepository(ApplicationDbContext context) : IMessageRepository
{
    private readonly TimeSpan _processingTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Adds a new message to the database.
    /// </summary>
    /// <param name="message">The message to add.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task AddAsync(Message message, CancellationToken ct)
    {
        context.Messages.Add(message);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Updates an existing message in the database.
    /// </summary>
    /// <param name="message">The message to update.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task UpdateAsync(Message message, CancellationToken ct)
    {
        context.Messages.Update(message);
        await context.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Retrieves queued messages ready for processing.
    /// Automatically handles stuck messages by resetting them for retry or dead-lettering if max attempts exceeded.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of messages ready for processing.</returns>
    public async Task<List<Message>> GetQueuedMessagesAsync(int batchSize, CancellationToken ct)
    {
        // Calculate cutoff time before the query (EF Core can't translate DateTime.Add in SQL)
        var cutoffTime = DateTime.UtcNow.Add(-_processingTimeout);

        // Reset stuck messages (those in Processing state beyond timeout)
        var stuckMessages = await context
            .Messages.Where(m =>
                m.Status == MessageStatus.Processing && m.ProcessingStartedAt <= cutoffTime
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var msg in stuckMessages)
        {
            if (msg.AttemptCount >= msg.MaxAttempts)
            {
                msg.MarkAsDeadLettered($"Max attempts ({msg.MaxAttempts}) exceeded after timeout");
            }
            else
            {
                msg.ResetForRetry();
            }
        }

        if (stuckMessages.Count > 0)
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        // Get fresh queued messages
        return await context
            .Messages.Include(message => message.Attachments)
            .Where(message =>
                message.Status == MessageStatus.Queued
                && (message.ScheduledFor == null || message.ScheduledFor <= DateTime.UtcNow)
            )
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves email addresses of all sent messages.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of recipient email addresses.</returns>
    public async Task<List<string>> GetSentToAddressesAsync(CancellationToken ct)
    {
        return await context
            .Messages.Where(e => e.SentAt != null)
            .Select(e => e.To)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves all dead-lettered messages ordered by creation date descending.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of dead-lettered messages.</returns>
    public async Task<List<Message>> GetDeadLetteredMessagesAsync(CancellationToken ct)
    {
        return await context
            .Messages.Where(m => m.Status == MessageStatus.DeadLettered)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the count of queued messages.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of queued messages.</returns>
    public async Task<int> GetQueuedCountAsync(CancellationToken ct)
    {
        return await context
            .Messages.CountAsync(e => e.Status == MessageStatus.Queued, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a message by its ID.
    /// </summary>
    /// <param name="id">The message ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The message if found, otherwise null.</returns>
    public async Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context
            .Messages.Include(m => m.Attachments)
            .FirstOrDefaultAsync(m => m.Id == id, ct)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets stuck messages that have been processing too long.
    /// </summary>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of stuck messages.</returns>
    public async Task<List<Message>> GetStuckMessagesAsync(
        TimeSpan timeout,
        CancellationToken ct = default
    )
    {
        var cutoffTime = DateTime.UtcNow.Add(-timeout);
        return await context
            .Messages.Where(m =>
                m.Status == MessageStatus.Processing && m.ProcessingStartedAt <= cutoffTime
            )
            .ToListAsync(ct)
            .ConfigureAwait(false);
    }
}
