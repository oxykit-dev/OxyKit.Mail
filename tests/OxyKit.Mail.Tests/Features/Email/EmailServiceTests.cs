using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using OxyKit.Mail.Contracts;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Tests.Features.Email;

public class EmailServiceTests
{
    private Mock<IMessageRepository> _messageRepoMock = null!;
    private Mock<ISenderService> _senderServiceMock = null!;
    private Mock<IHubContext<TestHub>> _hubContextMock = null!;
    private Mock<IHubClients> _hubClientsMock = null!;
    private Mock<IClientProxy> _clientProxyMock = null!;
    private EmailService<TestHub> _sut = null!;
    private CancellationToken _ct;

    public EmailServiceTests()
    {
        _messageRepoMock = new Mock<IMessageRepository>();
        _senderServiceMock = new Mock<ISenderService>();
        _hubContextMock = new Mock<IHubContext<TestHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();
        _ct = CancellationToken.None;

        _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
        _hubClientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);

        _messageRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _messageRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _messageRepoMock
            .Setup(x =>
                x.GetByIdempotencyKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Message?)null);

        _sut = new EmailService<TestHub>(
            _messageRepoMock.Object,
            _senderServiceMock.Object,
            Options.Create(
                new SmtpSettings
                {
                    Host = "localhost",
                    Port = 25,
                    UserName = "sender@example.com",
                    Password = "secret",
                }
            ),
            _hubContextMock.Object,
            Mock.Of<ILogger<EmailService<TestHub>>>()
        );
    }

    [Fact]
    public async Task AcceptAsync_ImmediateSendSuccess_ReturnsSent()
    {
        Message? persisted = null;
        var updateStatuses = new List<MessageStatus>();
        _messageRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => persisted = m)
            .Returns(Task.CompletedTask);
        _messageRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => updateStatuses.Add(m.Status))
            .Returns(Task.CompletedTask);

        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => m.MarkAsSent())
            .ReturnsAsync(Result.Success);

        var result = await _sut.AcceptAsync(ValidRequest(), ct: _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Sent);
        result.Value.EmailId.Should().NotBe(Guid.Empty);
        result.Value.EmailId.Should().Be(persisted!.Id);
        result.Value.Message.Should().Contain("sent");
        persisted.Status.Should().Be(MessageStatus.Sent);
        _messageRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        // Processing checkpoint before SMTP; Sent persistence is owned by SenderService.
        updateStatuses.Should().Equal(MessageStatus.Processing);
        _clientProxyMock.Verify(
            x => x.SendCoreAsync("EmailSent", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptAsync_ImmediateSendFailure_ReturnsAcceptedNotError()
    {
        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure(description: "SMTP down"));

        Message? queued = null;
        _messageRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => queued = m)
            .Returns(Task.CompletedTask);

        var result = await _sut.AcceptAsync(ValidRequest(), ct: _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Accepted);
        result.Value.EmailId.Should().NotBe(Guid.Empty);
        result.Value.Message.Should().Contain("queued");
        queued!.Status.Should().Be(MessageStatus.Queued);
        // Orchestrators must treat this as success — Mail owns retry.
        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _clientProxyMock.Verify(
            x =>
                x.SendCoreAsync("EmailQueued", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _clientProxyMock.Verify(
            x => x.SendCoreAsync("EmailSent", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AcceptAsync_ImmediateSendException_ReturnsAcceptedNotError()
    {
        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Message? queued = null;
        _messageRepoMock
            .Setup(x => x.UpdateAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => queued = m)
            .Returns(Task.CompletedTask);

        var result = await _sut.AcceptAsync(ValidRequest(), ct: _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Accepted);
        result.Value.EmailId.Should().NotBe(Guid.Empty);
        queued!.Status.Should().Be(MessageStatus.Queued);
        _clientProxyMock.Verify(
            x =>
                x.SendCoreAsync("EmailQueued", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptAsync_Scheduled_ReturnsScheduled()
    {
        var when = DateTime.UtcNow.AddHours(2);
        Message? added = null;
        _messageRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => added = m)
            .Returns(Task.CompletedTask);

        var request = ValidRequest();
        request.ScheduledFor = when;
        var result = await _sut.AcceptAsync(request, ct: _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Scheduled);
        result.Value.EmailId.Should().NotBe(Guid.Empty);
        result.Value.Message.Should().Contain("scheduled");
        added!.Status.Should().Be(MessageStatus.Queued);
        added.ScheduledFor.Should().BeCloseTo(when, TimeSpan.FromSeconds(1));
        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _clientProxyMock.Verify(
            x =>
                x.SendCoreAsync("EmailQueued", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AcceptAsync_MissingRequiredFields_ThrowsBeforePersist()
    {
        var request = ValidRequest();
        request.To = "";
        request.Subject = "";
        var act = () => _sut.AcceptAsync(request, ct: _ct);

        await act.Should().ThrowAsync<System.ComponentModel.DataAnnotations.ValidationException>();
        _messageRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AcceptAsync_SameIdempotencyKey_ReturnsExistingWithoutResend()
    {
        var existingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        _messageRepoMock
            .Setup(x => x.GetByIdempotencyKeyAsync("flow-job:0", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Message
                {
                    Id = existingId,
                    From = "sender@example.com",
                    To = "recipient@example.com",
                    Subject = "Hello",
                    Body = "Hi",
                    Status = MessageStatus.Sent,
                    SentAt = DateTime.UtcNow,
                }
            );

        var result = await _sut.AcceptAsync(ValidRequest(), "flow-job:0", _ct);

        result.IsError.Should().BeFalse();
        result.Value.EmailId.Should().Be(existingId);
        result.Value.Status.Should().Be(SendEmailStatus.Sent);
        _messageRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _senderServiceMock.Verify(
            x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AcceptAsync_IdempotencyKeyTooLong_ReturnsValidation()
    {
        var result = await _sut.AcceptAsync(ValidRequest(), new string('x', 129), _ct);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Validation);
        _messageRepoMock.Verify(
            x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task AcceptAsync_WithIdempotencyKey_PersistsKeyOnMessage()
    {
        Message? persisted = null;
        _messageRepoMock
            .Setup(x => x.AddAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => persisted = m)
            .Returns(Task.CompletedTask);
        _senderServiceMock
            .Setup(x => x.SendAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
            .Callback<Message, CancellationToken>((m, _) => m.MarkAsSent())
            .ReturnsAsync(Result.Success);

        var result = await _sut.AcceptAsync(ValidRequest(), "  my-key  ", _ct);

        result.IsError.Should().BeFalse();
        persisted!.IdempotencyKey.Should().Be("my-key");
    }

    [Fact]
    public async Task AcceptAsync_ExistingFutureSchedule_ReturnsScheduled()
    {
        var when = DateTime.UtcNow.AddHours(2);
        _messageRepoMock
            .Setup(x => x.GetByIdempotencyKeyAsync("sched-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Message
                {
                    Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                    From = "sender@example.com",
                    To = "recipient@example.com",
                    Subject = "Hello",
                    Body = "Hi",
                    Status = MessageStatus.Queued,
                    ScheduledFor = when,
                }
            );

        var result = await _sut.AcceptAsync(ValidRequest(), "sched-key", _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Scheduled);
    }

    [Fact]
    public async Task AcceptAsync_ExistingPastDueSchedule_ReturnsAccepted()
    {
        _messageRepoMock
            .Setup(x => x.GetByIdempotencyKeyAsync("due-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new Message
                {
                    Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                    From = "sender@example.com",
                    To = "recipient@example.com",
                    Subject = "Hello",
                    Body = "Hi",
                    Status = MessageStatus.Queued,
                    ScheduledFor = DateTime.UtcNow.AddMinutes(-5),
                }
            );

        var result = await _sut.AcceptAsync(ValidRequest(), "due-key", _ct);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(SendEmailStatus.Accepted);
    }

    private static SendEmailRequest ValidRequest() =>
        new()
        {
            To = "recipient@example.com",
            Cc = "",
            Bcc = "",
            Subject = "Hello",
            Body = "<p>Hi</p>",
            IsHtml = true,
            Attachments = null,
            ScheduledFor = null,
        };
}
