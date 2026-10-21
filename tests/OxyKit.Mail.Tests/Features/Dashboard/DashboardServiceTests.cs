using FluentAssertions;
using Moq;
using OxyKit.Mail.Features.Dashboard.Services;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;
using OxyKit.Mail.Tests.Utilities;

namespace OxyKit.Mail.Tests.Features.Dashboard;

/// <summary>
/// Unit tests for DashboardService.
/// </summary>
public class DashboardServiceTests
{
    private Mock<IMessageLogRepository> _messageLogRepoMock = null!;
    private Mock<IMessageRepository> _messageRepoMock = null!;
    private DashboardService _sut = null!;

    public DashboardServiceTests()
    {
        // Create mocks with loose behavior for easier testing
        _messageLogRepoMock = new Mock<IMessageLogRepository>();
        _messageRepoMock = new Mock<IMessageRepository>();
        _sut = new DashboardService(_messageLogRepoMock.Object, _messageRepoMock.Object);
    }

    #region GetEmailStatsAsync Tests

    [Fact]
    public async Task GetEmailStatsAsync_WithData_ReturnsCorrectStats()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((100, 10));
        _messageRepoMock
            .Setup(x => x.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);
        _messageLogRepoMock
            .Setup(x => x.GetLastEmailSentAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DateTime(2025, 1, 14, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var result = await _sut.GetEmailStatsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TotalEmailsSent.Should().Be(100);
        result.TotalEmailsFailed.Should().Be(10);
        result.TotalEmailsQueued.Should().Be(5);
        result.LastEmailSentAt.Should().Be(new DateTime(2025, 1, 14, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task GetEmailStatsAsync_WithNoData_ReturnsZeroStats()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));
        _messageRepoMock
            .Setup(x => x.GetQueuedCountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _messageLogRepoMock
            .Setup(x => x.GetLastEmailSentAtAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(DateTime.MinValue);

        // Act
        var result = await _sut.GetEmailStatsAsync(CancellationToken.None);

        // Assert
        result.TotalEmailsSent.Should().Be(0);
        result.TotalEmailsFailed.Should().Be(0);
        result.TotalEmailsQueued.Should().Be(0);
    }

    #endregion

    #region GetDeliverySuccessRateAsync Tests

    [Fact]
    public async Task GetDeliverySuccessRateAsync_WithNoEmails_ReturnsZeroRate()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((0, 0));

        // Act
        var result = await _sut.GetDeliverySuccessRateAsync(CancellationToken.None);

        // Assert
        result.SuccessRate.Should().Be(0);
        result.TotalSent.Should().Be(0);
        result.TotalFailed.Should().Be(0);
    }

    [Fact]
    public async Task GetDeliverySuccessRateAsync_With50PercentSuccess_ReturnsCorrectRate()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((50, 50));

        // Act
        var result = await _sut.GetDeliverySuccessRateAsync(CancellationToken.None);

        // Assert
        result.SuccessRate.Should().BeApproximately(50.0, 0.01);
        result.TotalSent.Should().Be(50);
        result.TotalFailed.Should().Be(50);
    }

    [Fact]
    public async Task GetDeliverySuccessRateAsync_With90PercentSuccess_ReturnsCorrectRate()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((90, 10));

        // Act
        var result = await _sut.GetDeliverySuccessRateAsync(CancellationToken.None);

        // Assert
        result.SuccessRate.Should().BeApproximately(90.0, 0.01);
        result.TotalSent.Should().Be(90);
        result.TotalFailed.Should().Be(10);
    }

    [Fact]
    public async Task GetDeliverySuccessRateAsync_With100PercentSuccess_Returns100Rate()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetDeliveryStatsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((100, 0));

        // Act
        var result = await _sut.GetDeliverySuccessRateAsync(CancellationToken.None);

        // Assert
        result.SuccessRate.Should().Be(100.0);
        result.TotalSent.Should().Be(100);
        result.TotalFailed.Should().Be(0);
    }

    #endregion

    #region GetLatestEmailsAsync Tests

    [Fact]
    public async Task GetLatestEmailsAsync_WithData_ReturnsCorrectCount()
    {
        // Arrange
        var logs = new List<MessageLog>
        {
            TestDataBuilder.CreateMessageLog(l => l.Message.Subject = "Email 1"),
            TestDataBuilder.CreateMessageLog(l => l.Message.Subject = "Email 2"),
        };
        _messageLogRepoMock
            .Setup(x => x.GetRecentAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(logs);

        // Act
        var result = await _sut.GetLatestEmailsAsync(2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].Subject.Should().Be("Email 1");
        result[1].Subject.Should().Be("Email 2");
    }

    [Fact]
    public async Task GetLatestEmailsAsync_WithNoData_ReturnsEmptyList()
    {
        // Arrange
        _messageLogRepoMock
            .Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetLatestEmailsAsync(10, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetMostRecentFailuresAsync Tests

    [Fact]
    public async Task GetMostRecentFailuresAsync_WithData_ReturnsOnlyFailures()
    {
        // Arrange
        var failures = new List<MessageLog>
        {
            TestDataBuilder.CreateMessageLog(l =>
            {
                l.Status = MessageStatus.Failed;
                l.ErrorMessage = "SMTP timeout";
            }),
            TestDataBuilder.CreateMessageLog(l =>
            {
                l.Status = MessageStatus.Failed;
                l.ErrorMessage = "Connection refused";
            }),
        };
        _messageLogRepoMock
            .Setup(x => x.GetRecentFailuresAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failures);

        // Act
        var result = await _sut.GetMostRecentFailuresAsync(2, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result
            .Should()
            .AllSatisfy(log =>
            {
                log.Status.Should().Be(MessageStatus.Failed.ToString());
                log.ErrorMessage.Should().NotBeNullOrEmpty();
            });
    }

    #endregion

    #region GetEmailsPerDayAsync Tests

    [Fact]
    public async Task GetEmailsPerDayAsync_WithData_ReturnsCorrectGrouping()
    {
        // Arrange
        var groupedData = new List<(DateTime Date, int EmailsSent)>
        {
            (new DateTime(2025, 1, 14), 100),
            (new DateTime(2025, 1, 13), 85),
            (new DateTime(2025, 1, 12), 92),
        };
        _messageLogRepoMock
            .Setup(x =>
                x.GetEmailsPerDayAsync(
                    It.IsAny<DateTime>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(groupedData);

        // Act
        var result = await _sut.GetEmailsPerDayAsync(
            DateTime.UtcNow.AddDays(-7),
            "UTC",
            CancellationToken.None
        );

        // Assert
        result.Should().HaveCount(3);
        result[0].Date.Should().Be(new DateTime(2025, 1, 14));
        result[0].EmailsSent.Should().Be(100);
    }

    #endregion

    #region GetTopRecipientsAsync Tests

    [Fact]
    public async Task GetTopRecipientsAsync_WithData_ReturnsTopRecipientsSorted()
    {
        // Arrange
        var emailAddresses = new List<string>
        {
            "user1@example.com,user2@example.com",
            "user1@example.com",
            "user3@example.com,user1@example.com",
        };
        _messageRepoMock
            .Setup(x => x.GetSentToAddressesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailAddresses);

        // Act
        var result = await _sut.GetTopRecipientsAsync(5, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result[0].Recipient.Should().Be("user1@example.com");
        result[0].EmailsReceived.Should().Be(3); // Most frequent
        result[1].EmailsReceived.Should().BeLessThan(3);
    }

    [Fact]
    public async Task GetTopRecipientsAsync_RespectsLimit()
    {
        // Arrange
        var emailAddresses = new List<string>
        {
            "user1@example.com",
            "user2@example.com",
            "user3@example.com",
            "user4@example.com",
            "user5@example.com",
            "user6@example.com",
        };
        _messageRepoMock
            .Setup(x => x.GetSentToAddressesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailAddresses);

        // Act
        var result = await _sut.GetTopRecipientsAsync(3, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
    }

    #endregion

    #region GetDeadLetteredEmailsAsync Tests

    [Fact]
    public async Task GetDeadLetteredEmailsAsync_WithData_ReturnsDeadLetteredMessages()
    {
        // Arrange
        var deadLettered = new List<Message>
        {
            TestDataBuilder.CreateMessage(m =>
            {
                m.Status = MessageStatus.DeadLettered;
                m.DeadLetterReason = "Max retries exceeded";
            }),
            TestDataBuilder.CreateMessage(m =>
            {
                m.Status = MessageStatus.DeadLettered;
                m.DeadLetterReason = "Invalid recipient";
            }),
        };
        _messageRepoMock
            .Setup(x => x.GetDeadLetteredMessagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(deadLettered);

        // Act
        var result = await _sut.GetDeadLetteredEmailsAsync(CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(msg => msg.DeadLetterReason.Should().NotBeNullOrEmpty());
    }

    [Fact]
    public async Task GetDeadLetteredEmailsAsync_WithNoData_ReturnsEmptyList()
    {
        // Arrange
        _messageRepoMock
            .Setup(x => x.GetDeadLetteredMessagesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _sut.GetDeadLetteredEmailsAsync(CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
