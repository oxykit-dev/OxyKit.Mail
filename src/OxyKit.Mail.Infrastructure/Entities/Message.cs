using System.ComponentModel.DataAnnotations;

namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Represents an email message with retry and idempotency tracking.
/// This is a rich domain entity with behavior for managing the message lifecycle.
/// </summary>
public class Message : EntityBase
{
    #region Properties

    /// <summary>
    /// Sender email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Primary recipient email address.
    /// </summary>
    [Required]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Carbon copy recipients (comma-separated).
    /// </summary>
    public string Cc { get; set; } = string.Empty;

    /// <summary>
    /// Blind carbon copy recipients (comma-separated).
    /// </summary>
    public string Bcc { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line.
    /// </summary>
    [Required]
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Email body content.
    /// </summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the body content is HTML formatted.
    /// </summary>
    public bool IsHtml { get; set; } = true;

    /// <summary>
    /// Timestamp when the message was successfully sent.
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Scheduled send time (null for immediate sending).
    /// </summary>
    public DateTime? ScheduledFor { get; set; }

    /// <summary>
    /// Collection of file attachments.
    /// </summary>
    public ICollection<Attachment> Attachments { get; set; } = [];

    /// <summary>
    /// Current status of the message in the processing pipeline.
    /// </summary>
    [Required]
    public MessageStatus Status { get; set; } = MessageStatus.Queued;

    /// <summary>
    /// Reason for dead-lettering or failure (if applicable).
    /// </summary>
    public string? DeadLetterReason { get; set; }

    /// <summary>
    /// Optional client-supplied key (e.g. <c>Idempotency-Key</c> header). Unique when set;
    /// same key returns the existing message instead of accepting another send.
    /// </summary>
    [MaxLength(128)]
    public string? IdempotencyKey { get; set; }

    #endregion

    #region Idempotency and Retry Tracking

    /// <summary>
    /// Number of send attempts made for this message.
    /// </summary>
    public int AttemptCount { get; set; } = 0;

    /// <summary>
    /// Maximum number of send attempts allowed before dead-lettering.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Timestamp when message processing started (moved to Processing status).
    /// </summary>
    public DateTime? ProcessingStartedAt { get; set; }

    /// <summary>
    /// Timestamp of the most recent send attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    #endregion

    #region Status Transition Methods

    /// <summary>
    /// Marks the message as queued and resets processing timestamps.
    /// </summary>
    public void MarkAsQueued()
    {
        Status = MessageStatus.Queued;
        SentAt = null;
        DeadLetterReason = null;
        ProcessingStartedAt = null;
    }

    /// <summary>
    /// Marks the message as processing and increments the attempt count.
    /// This method provides idempotency by tracking processing attempts.
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = MessageStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
        AttemptCount++;
        LastAttemptAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the message as successfully sent.
    /// </summary>
    public void MarkAsSent()
    {
        Status = MessageStatus.Sent;
        SentAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the message as failed with the specified reason.
    /// </summary>
    /// <param name="reason">Reason for the failure.</param>
    public void MarkAsFailed(string reason)
    {
        Status = MessageStatus.Failed;
        DeadLetterReason = reason;
    }

    /// <summary>
    /// Marks the message as dead-lettered with the specified reason.
    /// Dead-lettered messages will not be retried.
    /// </summary>
    /// <param name="reason">Reason for dead-lettering.</param>
    public void MarkAsDeadLettered(string reason)
    {
        Status = MessageStatus.DeadLettered;
        DeadLetterReason = reason;
    }

    #endregion

    #region Business Logic Methods

    /// <summary>
    /// Determines if the message can be sent (is in Queued or Processing status).
    /// </summary>
    /// <returns>True if the message can be sent, otherwise false.</returns>
    public bool CanBeSent() => Status == MessageStatus.Queued || Status == MessageStatus.Processing;

    /// <summary>
    /// Determines if the message can be retried after a failure.
    /// </summary>
    /// <returns>True if retry is possible (attempts below max and status is Failed or Queued), otherwise false.</returns>
    public bool CanBeRetried() =>
        AttemptCount < MaxAttempts
        && (Status == MessageStatus.Failed || Status == MessageStatus.Queued);

    /// <summary>
    /// Determines if the message is stuck in processing (exceeded timeout).
    /// Stuck messages are candidates for retry or dead-lettering.
    /// </summary>
    /// <param name="timeout">Timeout duration to consider a message stuck.</param>
    /// <returns>True if the message is stuck, otherwise false.</returns>
    public bool IsStuck(TimeSpan timeout) =>
        Status == MessageStatus.Processing
        && ProcessingStartedAt.HasValue
        && DateTime.UtcNow - ProcessingStartedAt.Value > timeout;

    /// <summary>
    /// Resets the message for retry by changing status back to Queued.
    /// Only resets if retry is possible.
    /// </summary>
    public void ResetForRetry()
    {
        if (CanBeRetried())
        {
            Status = MessageStatus.Queued;
            ProcessingStartedAt = null;
        }
    }

    #endregion

    #region Validation Methods

    /// <summary>
    /// Validates that the message has all required fields.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when required fields are missing.</exception>
    public void ThrowIfInvalid()
    {
        if (
            string.IsNullOrWhiteSpace(From)
            || string.IsNullOrWhiteSpace(To)
            || string.IsNullOrWhiteSpace(Subject)
            || string.IsNullOrWhiteSpace(Body)
        )
        {
            throw new ValidationException("Message is missing required fields.");
        }

        foreach (var attachment in Attachments)
            attachment.ThrowIfInvalid();
    }

    #endregion

    #region Attachment Management Methods

    /// <summary>
    /// Adds an attachment to the message.
    /// </summary>
    /// <param name="attachment">The attachment to add.</param>
    public void AddAttachment(Attachment attachment)
    {
        attachment.ThrowIfInvalid();
        Attachments.Add(attachment);
    }

    /// <summary>
    /// Removes an attachment from the message.
    /// </summary>
    /// <param name="attachment">The attachment to remove.</param>
    public void RemoveAttachment(Attachment attachment)
    {
        Attachments.Remove(attachment);
    }

    /// <summary>
    /// Checks if the message has an attachment with the specified file name.
    /// </summary>
    /// <param name="fileName">The file name to check.</param>
    /// <returns>True if an attachment with the file name exists, otherwise false.</returns>
    public bool HasAttachment(string fileName)
    {
        return Attachments.Any(a => a.FileName == fileName);
    }

    #endregion

    #region Status Query Methods

    /// <summary>
    /// Checks if the message is dead-lettered.
    /// </summary>
    /// <returns>True if dead-lettered, otherwise false.</returns>
    public bool IsDeadLettered() => Status == MessageStatus.DeadLettered;

    /// <summary>
    /// Checks if the message has failed.
    /// </summary>
    /// <returns>True if failed, otherwise false.</returns>
    public bool IsFailed() => Status == MessageStatus.Failed;

    /// <summary>
    /// Checks if the message was successfully sent.
    /// </summary>
    /// <returns>True if sent, otherwise false.</returns>
    public bool IsSent() => Status == MessageStatus.Sent;

    #endregion
}
