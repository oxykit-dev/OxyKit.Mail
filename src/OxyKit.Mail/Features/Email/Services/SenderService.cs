using ErrorOr;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Service interface for sending emails via SMTP.
/// </summary>
public interface ISenderService
{
    /// <summary>
    /// Sends an email message via SMTP with retry logic.
    /// </summary>
    /// <param name="emailMessage">The message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success or error.</returns>
    Task<ErrorOr<Success>> SendAsync(
        Message emailMessage,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
/// Service for sending emails with DKIM signing, retry, and dead-letter logic.
/// </summary>
public class SenderService : ISenderService
{
    private readonly SmtpSettings _smtpSettings;
    private readonly ApplicationDbContext _context;
    private readonly IMessageLogRepository _logRepository;
    private readonly int _maxRetries;
    private readonly int _delayMs;
    private readonly ILogger<SenderService> _logger;
    private readonly ISmtpClientService _smtpClientService;
    private readonly IDkimService _dkimService;
    private readonly ISmtpAccessTokenProvider _accessTokenProvider;

    public SenderService(
        IOptions<SmtpSettings> smtpSettings,
        ApplicationDbContext context,
        IMessageLogRepository logRepository,
        IOptions<EmailSenderSettings> senderSettings,
        ILogger<SenderService> logger,
        ISmtpClientService smtpClientService,
        IDkimService dkimService,
        ISmtpAccessTokenProvider accessTokenProvider
    )
    {
        _smtpSettings = smtpSettings.Value;
        _context = context;
        _logRepository = logRepository;
        var settings = senderSettings.Value;
        _maxRetries =
            settings.MaxRetries > 0
                ? settings.MaxRetries
                : throw new ArgumentException("MaxRetries must be positive");
        _delayMs =
            settings.DelayMs > 0
                ? settings.DelayMs
                : throw new ArgumentException("DelayMs must be positive");
        _logger = logger;
        _smtpClientService = smtpClientService;
        _dkimService = dkimService;
        _accessTokenProvider = accessTokenProvider;
    }

    /// <summary>
    /// Sends an email message with DKIM signing, retry, and dead-letter logic.
    /// </summary>
    public async Task<ErrorOr<Success>> SendAsync(
        Message emailMessage,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(emailMessage);

        using var mimeMessage = EmailMapper.ToMimeMessage(emailMessage);

        // Sign with DKIM if enabled
        if (_dkimService.IsEnabled())
        {
            var signResult = _dkimService.SignMessage(mimeMessage);
            if (signResult.IsError)
            {
                _logger.LogWarning(
                    "DKIM signing failed: {Error}. Continuing without signature.",
                    signResult.FirstError.Description
                );
            }
        }

        var emailLog = new MessageLog
        {
            MessageId = emailMessage.Id,
            Status = MessageStatus.Processing,
            LoggedAt = DateTime.UtcNow,
        };

        var (sent, lastSendException) = await TrySendWithRetryAsync(mimeMessage, cancellationToken);
        UpdateMessageStatus(emailMessage, emailLog, sent, lastSendException);
        await _logRepository.AddAsync(emailLog, cancellationToken);
        _context.Messages.Update(emailMessage);
        await _context.SaveChangesAsync(cancellationToken);

        if (!sent)
        {
            return Error.Failure(
                description: lastSendException?.Message ?? "Failed to send email."
            );
        }
        return Result.Success;
    }

    private async Task<(bool sent, Exception? lastSendException)> TrySendWithRetryAsync(
        MimeMessage mimeMessage,
        CancellationToken cancellationToken
    )
    {
        Exception? lastSendException = null;
        for (
            int attempt = 1;
            attempt <= _maxRetries && !cancellationToken.IsCancellationRequested;
            attempt++
        )
        {
            try
            {
                await _smtpClientService.ConnectAsync(
                    _smtpSettings.Host,
                    _smtpSettings.Port,
                    _smtpSettings.EnableSsl
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.None,
                    cancellationToken
                );
                await AuthenticateSmtpAsync(cancellationToken);
                await _smtpClientService.SendAsync(mimeMessage, cancellationToken);
                await _smtpClientService.DisconnectAsync(true, cancellationToken);
                _logger.LogInformation("Email sent successfully on attempt {Attempt}.", attempt);
                return (true, null);
            }
            catch (SmtpCommandException ex)
            {
                lastSendException = ex;
                _logger.LogWarning(
                    ex,
                    "SMTP command error on attempt {Attempt}: {Message}",
                    attempt,
                    ex.Message
                );
            }
            catch (SmtpProtocolException ex)
            {
                lastSendException = ex;
                _logger.LogWarning(
                    ex,
                    "SMTP protocol error on attempt {Attempt}: {Message}",
                    attempt,
                    ex.Message
                );
            }
            catch (Exception ex)
            {
                lastSendException = ex;
                _logger.LogWarning(
                    ex,
                    "General error on attempt {Attempt}: {Message}",
                    attempt,
                    ex.Message
                );
            }
            if (attempt < _maxRetries)
            {
                var delay = GetExponentialBackoff(attempt);
                _logger.LogInformation(
                    "Waiting {Delay}ms before next retry (attempt {Attempt})...",
                    delay,
                    attempt + 1
                );
                await Task.Delay(delay, cancellationToken);
            }
        }
        _logger.LogError(
            lastSendException,
            "Email could not be sent after {MaxRetries} attempts.",
            _maxRetries
        );
        return (false, lastSendException);
    }

    private async Task AuthenticateSmtpAsync(CancellationToken cancellationToken)
    {
        if (_smtpSettings.AuthMode == SmtpAuthMode.OAuth2)
        {
            var accessToken = await _accessTokenProvider.GetAccessTokenAsync(cancellationToken);
            await _smtpClientService.AuthenticateOAuth2Async(
                _smtpSettings.UserName,
                accessToken,
                cancellationToken
            );
            return;
        }

        await _smtpClientService.AuthenticateAsync(
            _smtpSettings.UserName,
            _smtpSettings.Password,
            cancellationToken
        );
    }

    private int GetExponentialBackoff(int attempt) => _delayMs * attempt;

    private static void UpdateMessageStatus(
        Message emailMessage,
        MessageLog emailLog,
        bool sent,
        Exception? lastSendException
    )
    {
        if (sent)
        {
            emailMessage.MarkAsSent();
            emailLog.Status = MessageStatus.Sent;
        }
        else
        {
            emailMessage.MarkAsDeadLettered(lastSendException?.ToString() ?? "Unknown error");
            emailLog.Status = MessageStatus.DeadLettered;
            emailLog.ErrorMessage = lastSendException?.ToString();
        }
    }
}
