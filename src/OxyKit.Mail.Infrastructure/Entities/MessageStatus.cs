namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Represents the current status of an email message in the processing pipeline.
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Message is queued and waiting to be processed.
    /// </summary>
    Queued,

    /// <summary>
    /// Message is currently being processed/sent.
    /// </summary>
    Processing,

    /// <summary>
    /// Message has been successfully sent.
    /// </summary>
    Sent,

    /// <summary>
    /// Message sending failed but may be retried.
    /// </summary>
    Failed,

    /// <summary>
    /// Message has exceeded max retry attempts and will not be retried.
    /// </summary>
    DeadLettered,
}
