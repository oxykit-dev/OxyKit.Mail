using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;
using OxyKit.Mail.Tests.Utilities;

namespace OxyKit.Mail.Tests.Features.Email;

/// <summary>
/// Test Hub for SignalR mocking.
/// </summary>
public class TestHub : Hub;

/// <summary>
/// Unit tests for EmailQueueProcessorService - the core email processing engine.
/// Tests cover queue processing, retry logic, dead lettering, and idempotency.
/// </summary>
public class EmailQueueProcessorServiceTests
{
    private Mock<IMessageRepository> _messageRepoMock = null!;
    private Mock<ISenderService> _senderServiceMock = null!;
    private Mock<ILogger<EmailQueueProcessorService<TestHub>>> _loggerMock = null!;
    private Mock<IHubContext<TestHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;
    private EmailQueueProcessorService<TestHub> _sut = null!;
    private CancellationToken _ct;

    public EmailQueueProcessorServiceTests()
    {
        _messageRepoMock = new Mock<IMessageRepository>();
        _senderServiceMock = new Mock<ISenderService>();
        _loggerMock = new Mock<ILogger<EmailQueueProcessorService<TestHub>>>();
        _hubContextMock = new Mock<IHubContext<TestHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _ct = CancellationToken.None;

        // Setup SignalR mocks
        _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);

        _sut = new EmailQueueProcessorService<TestHub>(
            _messageRepoMock.Object,
            _senderServiceMock.Object,
            _loggerMock.Object,
            _hubContextMock.Object
        );
    }

    #region ProcessQueueAsync - Happy Path

    [Fact]
    public async Task ProcessQueueAsync_WithNoMessages_ShouldNotProcessAnything()
    {
        // Arrange
        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _messageRepoMock.Verify(
            x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ProcessQueueAsync_WithSingleMessage_ShouldProcessSuccessfully()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 0;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        message.Status.Should().Be(MessageStatus.Sent);
        message.SentAt.Should().NotBeNull();
        message.AttemptCount.Should().Be(1);

        _messageRepoMock.Verify(
            x => x.UpdateAsync(message, _ct),
            Times.Exactly(2), // Once for MarkAsProcessing, once for MarkAsSent
            "Should save after marking as processing and after marking as sent"
        );

        _senderServiceMock.Verify(
            x => x.SendAsync(message, _ct),
            Times.Once,
            "Should attempt to send the message"
        );
    }

    [Fact]
    public async Task ProcessQueueAsync_WithMultipleMessages_ShouldProcessAll()
    {
        // Arrange
        var messages = new List<Message>
        {
            TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued),
            TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued),
            TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued),
        };

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        messages
            .Should()
            .AllSatisfy(m =>
            {
                m.Status.Should().Be(MessageStatus.Sent);
                m.SentAt.Should().NotBeNull();
                m.AttemptCount.Should().Be(1);
            });

        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3),
            "Should send all 3 messages"
        );
    }

    #endregion

    #region ProcessQueueAsync - Retry Logic

    [Fact]
    public async Task ProcessQueueAsync_WhenSendFails_ShouldDeadLetterDueToStatusCheck()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 0;
            m.MaxAttempts = 3;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("SMTP connection failed"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        message.Status.Should().Be(MessageStatus.DeadLettered);
        message.AttemptCount.Should().Be(1);
        message.SentAt.Should().BeNull();
        // Deadletter reason contains max attempts message
        message.DeadLetterReason.Should().Contain("SMTP connection failed");

        _messageRepoMock.Verify(
            x => x.UpdateAsync(message, _ct),
            Times.Exactly(2), // MarkAsProcessing + MarkAsFailed
            "Should save after processing attempt and after marking as failed"
        );
    }

    [Fact]
    public async Task ProcessQueueAsync_WhenSecondAttemptFails_ShouldDeadLetterWhenNotRetriable()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 1; // Already failed once
            m.MaxAttempts = 3;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Timeout"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        // After MarkAsProcessing (increment to 2), status is Processing
        // CanBeRetried checks: AttemptCount(2) < MaxAttempts(3) && (Status==Failed || Status==Queued)
        // = true && false (Processing != Failed/Queued) = false
        // Therefore, message is dead lettered, not marked for retry
        message.Status.Should().Be(MessageStatus.DeadLettered);
        message.AttemptCount.Should().Be(2);
        message.DeadLetterReason.Should().Contain("Max attempts");
    }

    #endregion

    #region ProcessQueueAsync - Dead Lettering

    [Fact]
    public async Task ProcessQueueAsync_WhenMaxAttemptsReached_ShouldDeadLetter()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 2; // Already failed twice
            m.MaxAttempts = 3;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Permanent failure"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        message.Status.Should().Be(MessageStatus.DeadLettered);
        message.AttemptCount.Should().Be(3);
        message.DeadLetterReason.Should().Contain("Max attempts (3) exceeded");
        message.DeadLetterReason.Should().Contain("Permanent failure");
        message.CanBeRetried().Should().BeFalse("Should have no attempts remaining");
    }

    [Fact]
    public async Task ProcessQueueAsync_WhenDeadLettered_ShouldNotRetryAgain()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 2;
            m.MaxAttempts = 3;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Final attempt failed"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        message.Status.Should().Be(MessageStatus.DeadLettered);
        message.CanBeRetried().Should().BeFalse();

        // Verify we don't try to send again
        _senderServiceMock.Verify(
            x => x.SendAsync(message, _ct),
            Times.Once,
            "Should only attempt to send once before dead lettering"
        );
    }

    #endregion

    #region ProcessQueueAsync - Idempotency

    [Fact]
    public async Task ProcessQueueAsync_ShouldMarkAsProcessingImmediately()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued);

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        var updateCallOrder = new List<MessageStatus>();
        _messageRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, ct) => updateCallOrder.Add(m.Status))
            .Returns(Task.CompletedTask);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        updateCallOrder.Should().HaveCount(2);
        updateCallOrder[0]
            .Should()
            .Be(MessageStatus.Processing, "First save should mark as Processing");
        updateCallOrder[1].Should().Be(MessageStatus.Sent, "Second save should mark as Sent");
    }

    [Fact]
    public async Task ProcessQueueAsync_ShouldIncrementAttemptCountBeforeSending()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.AttemptCount = 0;
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        var attemptCountWhenSending = 0;
        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>(
                (m, ct) => attemptCountWhenSending = m.AttemptCount
            )
            .ReturnsAsync(Result.Success);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        attemptCountWhenSending
            .Should()
            .Be(1, "Attempt count should be incremented before sending");
    }

    #endregion

    #region ProcessQueueAsync - SignalR Notifications

    [Fact]
    public async Task ProcessQueueAsync_WhenEmailSent_ShouldBroadcastNotification()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued);

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success);

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync("EmailSent", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should broadcast EmailSent event via SignalR"
        );
    }

    [Fact]
    public async Task ProcessQueueAsync_WhenEmailFails_ShouldNotBroadcastNotification()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued);

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Send failed"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        _clientProxyMock.Verify(
            x => x.SendCoreAsync("EmailSent", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not broadcast EmailSent when send fails"
        );
    }

    #endregion

    #region ProcessQueueAsync - Batch Size

    [Fact]
    public async Task ProcessQueueAsync_ShouldRespectBatchSize()
    {
        // Arrange
        const int batchSize = 5;

        _messageRepoMock.Setup(x => x.GetQueuedMessagesAsync(batchSize, _ct)).ReturnsAsync([]);

        // Act
        await _sut.ProcessQueueAsync(batchSize, _ct);

        // Assert
        _messageRepoMock.Verify(
            x => x.GetQueuedMessagesAsync(batchSize, _ct),
            Times.Once,
            "Should request exactly the specified batch size"
        );
    }

    #endregion

    #region ProcessQueueAsync - Mixed Success and Failure

    [Fact]
    public async Task ProcessQueueAsync_WithMixedResults_ShouldProcessAllMessages()
    {
        // Arrange
        var message1 = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.To = "success@test.com";
        });
        var message2 = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.To = "fail@test.com";
            m.AttemptCount = 0; // First attempt, will dead letter on first failure
        });
        var message3 = TestDataBuilder.CreateMessage(m =>
        {
            m.Status = MessageStatus.Queued;
            m.To = "success2@test.com";
        });

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message1, message2, message3]);

        _senderServiceMock
            .Setup(x =>
                x.SendAsync(
                    It.Is<Message>(m => m.To.Contains("success")),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success);

        _senderServiceMock
            .Setup(x =>
                x.SendAsync(
                    It.Is<Message>(m => m.To.Contains("fail")),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new Exception("Failed"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        message1.Status.Should().Be(MessageStatus.Sent);
        // CanBeRetried() returns false when status is Processing (not Failed/Queued)
        message2.Status.Should().Be(MessageStatus.DeadLettered);
        message3.Status.Should().Be(MessageStatus.Sent);

        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3),
            "Should attempt to send all 3 messages despite one failure"
        );
    }

    #endregion

    #region ProcessQueueAsync - Error Handling

    [Fact]
    public async Task ProcessQueueAsync_WhenExceptionOccurs_ShouldStillSaveState()
    {
        // Arrange
        var message = TestDataBuilder.CreateMessage(m => m.Status = MessageStatus.Queued);

        _messageRepoMock
            .Setup(x => x.GetQueuedMessagesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([message]);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Critical error"));

        // Act
        await _sut.ProcessQueueAsync(10, _ct);

        // Assert
        _messageRepoMock.Verify(
            x => x.UpdateAsync(message, _ct),
            Times.Exactly(2), // MarkAsProcessing + MarkAsFailed
            "Should save state even when send fails"
        );
    }

    #endregion
}
