using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Repositories;
using OxyKit.Mail.Tests.Utilities;

namespace OxyKit.Mail.Tests.Features.Email;

public class SenderServiceSmtpAuthTests
{
    [Fact]
    public async Task SendAsync_PasswordMode_UsesPasswordAuth()
    {
        var smtp = new Mock<ISmtpClientService>();
        smtp.Setup(s =>
                s.ConnectAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<MailKit.Security.SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        smtp.Setup(s =>
                s.AuthenticateAsync("user@example.com", "secret", It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);
        smtp.Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtp.Setup(s => s.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tokenProvider = new Mock<ISmtpAccessTokenProvider>(MockBehavior.Strict);
        var (sut, db) = CreateSut(
            new SmtpSettings
            {
                Host = "smtp.example.com",
                Port = 587,
                EnableSsl = true,
                UserName = "user@example.com",
                Password = "secret",
                AuthMode = SmtpAuthMode.Password,
            },
            smtp.Object,
            tokenProvider.Object
        );

        var message = TestDataBuilder.CreateMessage();
        db.Messages.Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.SendAsync(message, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        smtp.Verify(
            s => s.AuthenticateAsync("user@example.com", "secret", It.IsAny<CancellationToken>()),
            Times.Once
        );
        tokenProvider.Verify(
            t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task SendAsync_OAuth2Mode_UsesXOAuth2()
    {
        var smtp = new Mock<ISmtpClientService>();
        smtp.Setup(s =>
                s.ConnectAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<MailKit.Security.SecureSocketOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        smtp.Setup(s =>
                s.AuthenticateOAuth2Async(
                    "sender@contoso.com",
                    "access-token",
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);
        smtp.Setup(s => s.SendAsync(It.IsAny<MimeKit.MimeMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        smtp.Setup(s => s.DisconnectAsync(true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var tokenProvider = new Mock<ISmtpAccessTokenProvider>();
        tokenProvider
            .Setup(t => t.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("access-token");

        var (sut, db) = CreateSut(
            new SmtpSettings
            {
                Host = "smtp.office365.com",
                Port = 587,
                EnableSsl = true,
                UserName = "sender@contoso.com",
                AuthMode = SmtpAuthMode.OAuth2,
                OAuth2 = new SmtpOAuth2Settings
                {
                    Provider = SmtpOAuth2Provider.Microsoft,
                    GrantType = SmtpOAuth2GrantType.RefreshToken,
                    TenantId = "tenant",
                    ClientId = "client",
                    ClientSecret = "secret",
                    RefreshToken = "refresh",
                },
            },
            smtp.Object,
            tokenProvider.Object
        );

        var message = TestDataBuilder.CreateMessage();
        db.Messages.Add(message);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await sut.SendAsync(message, TestContext.Current.CancellationToken);

        result.IsError.Should().BeFalse();
        smtp.Verify(
            s =>
                s.AuthenticateOAuth2Async(
                    "sender@contoso.com",
                    "access-token",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        smtp.Verify(
            s =>
                s.AuthenticateAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    private static (SenderService Sut, ApplicationDbContext Db) CreateSut(
        SmtpSettings smtpSettings,
        ISmtpClientService smtpClient,
        ISmtpAccessTokenProvider tokenProvider
    )
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        var logRepo = new Mock<IMessageLogRepository>();
        logRepo
            .Setup(r =>
                r.AddAsync(
                    It.IsAny<OxyKit.Mail.Infrastructure.Entities.MessageLog>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        var dkim = new Mock<IDkimService>();
        dkim.Setup(d => d.IsEnabled()).Returns(false);

        var sut = new SenderService(
            Options.Create(smtpSettings),
            db,
            logRepo.Object,
            Options.Create(new EmailSenderSettings { MaxRetries = 1, DelayMs = 1 }),
            NullLogger<SenderService>.Instance,
            smtpClient,
            dkim.Object,
            tokenProvider
        );
        return (sut, db);
    }
}
