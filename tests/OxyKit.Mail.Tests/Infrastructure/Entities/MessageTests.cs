using System.ComponentModel.DataAnnotations;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Tests.Infrastructure.Entities;

/// <summary>
/// Unit tests for Message entity including idempotency and retry logic.
/// </summary>
public class MessageTests
{
    [Fact]
    public void Message_DefaultValues_AreCorrect()
    {
        var msg = new Message();
        Assert.Equal(string.Empty, msg.From);
        Assert.Equal(string.Empty, msg.To);
        Assert.Equal(string.Empty, msg.Subject);
        Assert.Equal(string.Empty, msg.Body);
        Assert.Equal(MessageStatus.Queued, msg.Status);
        Assert.True(msg.IsHtml);
        Assert.NotNull(msg.Attachments);
        Assert.Empty(msg.Attachments);

        // New idempotency fields
        Assert.Equal(0, msg.AttemptCount);
        Assert.Equal(3, msg.MaxAttempts);
        Assert.Null(msg.ProcessingStartedAt);
        Assert.Null(msg.LastAttemptAt);
    }

    [Fact]
    public void MarkAsSent_SetsStatusAndSentAt()
    {
        var msg = new Message();
        msg.MarkAsSent();
        Assert.Equal(MessageStatus.Sent, msg.Status);
        Assert.NotNull(msg.SentAt);
        Assert.True(msg.IsSent());
    }

    [Fact]
    public void MarkAsFailed_SetsStatusAndReason()
    {
        var msg = new Message();
        msg.MarkAsFailed("error");
        Assert.Equal(MessageStatus.Failed, msg.Status);
        Assert.Equal("error", msg.DeadLetterReason);
        Assert.True(msg.IsFailed());
    }

    [Fact]
    public void AddAttachment_ValidAttachment_AttachmentAdded()
    {
        var msg = new Message();
        var att = new Attachment
        {
            FileName = "file.txt",
            Content = [1],
            ContentType = "text/plain",
        };
        msg.AddAttachment(att);
        Assert.Single(msg.Attachments);
        Assert.True(msg.HasAttachment("file.txt"));
    }

    [Fact]
    public void AddAttachment_InvalidAttachment_Throws()
    {
        var msg = new Message();
        var att = new Attachment
        {
            FileName = "",
            Content = [],
            ContentType = "",
        };
        Assert.Throws<ValidationException>(() => msg.AddAttachment(att));
    }

    [Fact]
    public void ThrowIfInvalid_MissingRequiredFields_Throws()
    {
        var msg = new Message
        {
            From = "",
            To = "",
            Subject = "",
            Body = "",
        };
        Assert.Throws<ValidationException>(() => msg.ThrowIfInvalid());
    }

    [Fact]
    public void CanBeSent_QueuedOrProcessing_ReturnsTrue()
    {
        var msg = new Message { Status = MessageStatus.Queued };
        Assert.True(msg.CanBeSent());
        msg.Status = MessageStatus.Processing;
        Assert.True(msg.CanBeSent());
    }

    [Fact]
    public void CanBeSent_OtherStatuses_ReturnsFalse()
    {
        var msg = new Message { Status = MessageStatus.Sent };
        Assert.False(msg.CanBeSent());
        msg.Status = MessageStatus.Failed;
        Assert.False(msg.CanBeSent());
        msg.Status = MessageStatus.DeadLettered;
        Assert.False(msg.CanBeSent());
    }

    #region Idempotency Tests (Phase 1)

    [Fact]
    public void MarkAsProcessing_IncrementsAttemptCount()
    {
        // Arrange
        var msg = CreateTestMessage();
        var initialCount = msg.AttemptCount;

        // Act
        msg.MarkAsProcessing();

        // Assert
        Assert.Equal(initialCount + 1, msg.AttemptCount);
    }

    [Fact]
    public void MarkAsProcessing_SetsProcessingStatus()
    {
        // Arrange
        var msg = CreateTestMessage();

        // Act
        msg.MarkAsProcessing();

        // Assert
        Assert.Equal(MessageStatus.Processing, msg.Status);
    }

    [Fact]
    public void MarkAsProcessing_SetsProcessingStartedAt()
    {
        // Arrange
        var msg = CreateTestMessage();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        msg.MarkAsProcessing();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(msg.ProcessingStartedAt);
        Assert.True(msg.ProcessingStartedAt > before);
        Assert.True(msg.ProcessingStartedAt < after);
    }

    [Fact]
    public void MarkAsProcessing_SetsLastAttemptAt()
    {
        // Arrange
        var msg = CreateTestMessage();
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        msg.MarkAsProcessing();
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.NotNull(msg.LastAttemptAt);
        Assert.True(msg.LastAttemptAt > before);
        Assert.True(msg.LastAttemptAt < after);
    }

    [Fact]
    public void MarkAsProcessing_IncrementsWithMultipleAttempts()
    {
        // Arrange
        var msg = CreateTestMessage();

        // Act
        msg.MarkAsProcessing();
        msg.MarkAsProcessing();
        msg.MarkAsProcessing();

        // Assert
        Assert.Equal(3, msg.AttemptCount);
    }

    [Fact]
    public void CanBeRetried_ReturnsTrueWhenAttemptsRemaining()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 2;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;

        // Act
        var result = msg.CanBeRetried();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeRetried_ReturnsFalseWhenMaxAttemptsExceeded()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 3;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;

        // Act
        var result = msg.CanBeRetried();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBeRetried_ReturnsTrueWhenQueued()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 1;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Queued;

        // Act
        var result = msg.CanBeRetried();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanBeRetried_ReturnsFalseWhenSent()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 1;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Sent;

        // Act
        var result = msg.CanBeRetried();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanBeRetried_ReturnsFalseWhenDeadLettered()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 1;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.DeadLettered;

        // Act
        var result = msg.CanBeRetried();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStuck_ReturnsTrueWhenExceedsTimeout()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.Status = MessageStatus.Processing;
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-10);
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var result = msg.IsStuck(timeout);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsStuck_ReturnsFalseWhenWithinTimeout()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.Status = MessageStatus.Processing;
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-2);
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var result = msg.IsStuck(timeout);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStuck_ReturnsFalseWhenNotProcessing()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.Status = MessageStatus.Queued;
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-10);
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var result = msg.IsStuck(timeout);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsStuck_ReturnsFalseWhenProcessingStartedAtIsNull()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.Status = MessageStatus.Processing;
        msg.ProcessingStartedAt = null;
        var timeout = TimeSpan.FromMinutes(5);

        // Act
        var result = msg.IsStuck(timeout);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ResetForRetry_ChangesStatusToQueuedWhenEligible()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 1;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        msg.ResetForRetry();

        // Assert
        Assert.Equal(MessageStatus.Queued, msg.Status);
    }

    [Fact]
    public void ResetForRetry_ClearsProcessingStartedAtWhenEligible()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 1;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-10);

        // Act
        msg.ResetForRetry();

        // Assert
        Assert.Null(msg.ProcessingStartedAt);
    }

    [Fact]
    public void ResetForRetry_DoesNotChangeStatusWhenMaxAttemptsExceeded()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 3;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;

        // Act
        msg.ResetForRetry();

        // Assert
        Assert.Equal(MessageStatus.Failed, msg.Status);
    }

    [Fact]
    public void ResetForRetry_PreservesAttemptCount()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.AttemptCount = 2;
        msg.MaxAttempts = 3;
        msg.Status = MessageStatus.Failed;

        // Act
        msg.ResetForRetry();

        // Assert
        Assert.Equal(2, msg.AttemptCount);
    }

    [Fact]
    public void MarkAsQueued_ClearsProcessingStartedAt()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.ProcessingStartedAt = DateTime.UtcNow;

        // Act
        msg.MarkAsQueued();

        // Assert
        Assert.Null(msg.ProcessingStartedAt);
    }

    /// <summary>
    /// Integration test: Simulates full message processing lifecycle with retries.
    /// </summary>
    [Fact]
    public void Scenario_MessageProcessingWithRetries_TracksAttemptsCorrectly()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.MaxAttempts = 3;

        // Act & Assert - First attempt
        msg.MarkAsProcessing();
        Assert.Equal(1, msg.AttemptCount);
        Assert.Equal(MessageStatus.Processing, msg.Status);

        // Simulate failure
        msg.MarkAsFailed("Network error");
        Assert.Equal(MessageStatus.Failed, msg.Status);
        Assert.True(msg.CanBeRetried());

        // Reset for retry
        msg.ResetForRetry();
        Assert.Equal(MessageStatus.Queued, msg.Status);

        // Second attempt
        msg.MarkAsProcessing();
        Assert.Equal(2, msg.AttemptCount);

        // Simulate failure again
        msg.MarkAsFailed("Timeout");
        Assert.True(msg.CanBeRetried());

        // Third attempt
        msg.ResetForRetry();
        msg.MarkAsProcessing();
        Assert.Equal(3, msg.AttemptCount);

        // Final failure
        msg.MarkAsFailed("Connection refused");
        Assert.False(msg.CanBeRetried(), "Max attempts reached");

        // Should be dead-lettered
        msg.MarkAsDeadLettered("Max attempts exceeded");
        Assert.Equal(MessageStatus.DeadLettered, msg.Status);
    }

    /// <summary>
    /// Integration test: Simulates stuck message detection and recovery.
    /// </summary>
    [Fact]
    public void Scenario_StuckMessage_IsDetectedAndReset()
    {
        // Arrange
        var msg = CreateTestMessage();
        msg.MarkAsProcessing();
        msg.ProcessingStartedAt = DateTime.UtcNow.AddMinutes(-10);
        var timeout = TimeSpan.FromMinutes(5);

        // Act & Assert
        Assert.True(msg.IsStuck(timeout), "Processing started 10 minutes ago");

        // Mark as failed first (stuck messages would be marked as failed)
        msg.MarkAsFailed("Stuck - exceeded timeout");
        Assert.True(msg.CanBeRetried(), "Only 1 attempt so far");

        // Reset for retry
        msg.ResetForRetry();
        Assert.Equal(MessageStatus.Queued, msg.Status);
        Assert.Null(msg.ProcessingStartedAt);
    }

    /// <summary>
    /// Integration test: Successful message send.
    /// </summary>
    [Fact]
    public void Scenario_SuccessfulSend_MarksAsSent()
    {
        // Arrange
        var msg = CreateTestMessage();

        // Act
        msg.MarkAsProcessing();
        msg.MarkAsSent();

        // Assert
        Assert.Equal(MessageStatus.Sent, msg.Status);
        Assert.NotNull(msg.SentAt);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test message with required fields populated.
    /// </summary>
    private static Message CreateTestMessage()
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            From = "test@example.com",
            To = "recipient@example.com",
            Subject = "Test Subject",
            Body = "Test Body",
            Status = MessageStatus.Queued,
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = DateTime.UtcNow,
        };
    }
    #endregion
}
