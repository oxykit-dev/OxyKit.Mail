using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Repository contract for Message entity operations.
/// </summary>
public interface IMessageRepository
{
    /// <summary>
    /// Adds a new message to the repository.
    /// </summary>
    Task AddAsync(Message message, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing message.
    /// </summary>
    Task UpdateAsync(Message message, CancellationToken ct = default);

    /// <summary>
    /// Gets a message by its ID.
    /// </summary>
    Task<Message?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets queued messages ready for processing.
    /// </summary>
    Task<List<Message>> GetQueuedMessagesAsync(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Gets the count of queued messages.
    /// </summary>
    Task<int> GetQueuedCountAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets all dead-lettered messages.
    /// </summary>
    Task<List<Message>> GetDeadLetteredMessagesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets stuck messages that have been processing too long.
    /// </summary>
    Task<List<Message>> GetStuckMessagesAsync(TimeSpan timeout, CancellationToken ct = default);

    /// <summary>
    /// Gets all sent recipient addresses for statistics.
    /// </summary>
    Task<List<string>> GetSentToAddressesAsync(CancellationToken ct = default);
}
