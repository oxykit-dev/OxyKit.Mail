using Microsoft.AspNetCore.SignalR;
using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Service interface for processing the email queue in the background.
/// </summary>
public interface IEmailQueueProcessorService
{
    /// <summary>
    /// Processes queued emails in batches.
    /// </summary>
    /// <param name="batchSize">Number of messages to process in one batch.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ProcessQueueAsync(int batchSize, CancellationToken ct);
}

/// <summary>
/// Service for processing queued email messages with retry logic and idempotency.
/// </summary>
public class EmailQueueProcessorService<THub>(
    IMessageRepository messageRepository,
    ISenderService senderService,
    ILogger<EmailQueueProcessorService<THub>> logger,
    IHubContext<THub> hubContext
) : IEmailQueueProcessorService
    where THub : Hub
{
    /// <summary>
    /// Processes a batch of queued messages with retry logic and idempotency protection.
    /// </summary>
    public async Task ProcessQueueAsync(int batchSize, CancellationToken stoppingToken)
    {
        var queuedMessages = await messageRepository
            .GetQueuedMessagesAsync(batchSize, stoppingToken)
            .ConfigureAwait(false);

        if (queuedMessages.Count > 0)
        {
            logger.LogInformation(
                "Picked up {Count} queued messages for processing.",
                queuedMessages.Count
            );
        }

        foreach (var message in queuedMessages)
        {
            // Mark as processing and save immediately (idempotency)
            message.MarkAsProcessing();
            await messageRepository.UpdateAsync(message, stoppingToken).ConfigureAwait(false);

            try
            {
                logger.LogInformation(
                    "Sending message {MessageId} (attempt {Attempt}/{Max})...",
                    message.Id,
                    message.AttemptCount,
                    message.MaxAttempts
                );

                await senderService.SendAsync(message, stoppingToken).ConfigureAwait(false);

                message.MarkAsSent();
                await messageRepository.UpdateAsync(message, stoppingToken).ConfigureAwait(false);

                logger.LogInformation("Message {MessageId} sent successfully.", message.Id);
                await hubContext
                    .Clients.All.SendAsync("EmailSent", stoppingToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send message {MessageId}", message.Id);

                // Handle retry logic based on attempt count
                if (message.CanBeRetried())
                {
                    message.MarkAsFailed($"Attempt {message.AttemptCount} failed: {ex.Message}");
                    logger.LogWarning(
                        "Message {MessageId} marked for retry ({Attempt}/{Max})",
                        message.Id,
                        message.AttemptCount,
                        message.MaxAttempts
                    );
                }
                else
                {
                    message.MarkAsDeadLettered(
                        $"Max attempts ({message.MaxAttempts}) exceeded. Last error: {ex.Message}"
                    );
                    logger.LogError(
                        "Message {MessageId} dead-lettered after {Attempts} attempts",
                        message.Id,
                        message.AttemptCount
                    );
                }

                await messageRepository.UpdateAsync(message, stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
