using ErrorOr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OxyKit.Mail.Contracts;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Infrastructure.Repositories;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Accepts email for Mail-owned delivery (immediate SMTP, queue retry, or schedule).
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Accepts an email for delivery. On SMTP failure, queues for Mail retry and
    /// returns <see cref="SendEmailStatus.Accepted"/> (not an error).
    /// </summary>
    Task<ErrorOr<SendEmailResponse>> AcceptAsync(SendEmailRequest request, CancellationToken ct);
}

/// <summary>
/// Dual-path email accept: immediate when unscheduled, queue when ScheduledFor is set.
/// On immediate SMTP failure, queues for Mail-side retry and returns Accepted.
/// </summary>
public sealed class EmailService<THub>(
    IMessageRepository messageRepository,
    ISenderService senderService,
    IOptions<SmtpSettings> smtpSettings,
    IHubContext<THub> hubContext,
    ILogger<EmailService<THub>> logger
) : IEmailService
    where THub : Hub
{
    private readonly SmtpSettings _smtpSettings = smtpSettings.Value;

    /// <inheritdoc />
    public async Task<ErrorOr<SendEmailResponse>> AcceptAsync(
        SendEmailRequest request,
        CancellationToken ct
    )
    {
        var message = EmailMapper.MapFromRequest(request, _smtpSettings);
        message.ThrowIfInvalid();

        if (message.ScheduledFor == null)
        {
            logger.LogInformation(
                "Sending email immediately to {To} with subject: {Subject}",
                message.To,
                message.Subject
            );

            await messageRepository.AddAsync(message, ct);

            try
            {
                message.MarkAsProcessing();
                await messageRepository.UpdateAsync(message, ct);

                var result = await senderService.SendAsync(message, ct);

                if (result.IsError)
                {
                    logger.LogWarning(
                        "Immediate send failed for message {MessageId}, queuing for Mail retry",
                        message.Id
                    );
                    message.MarkAsQueued();
                    await messageRepository.UpdateAsync(message, ct);
                    await hubContext.Clients.All.SendAsync("EmailQueued", ct);

                    return new SendEmailResponse
                    {
                        EmailId = message.Id,
                        Status = SendEmailStatus.Accepted,
                        Message = "Email accepted and queued for retry after initial send failure",
                    };
                }

                logger.LogInformation("Email sent immediately to {To}", message.To);
                await hubContext.Clients.All.SendAsync("EmailSent", ct);
                return new SendEmailResponse
                {
                    EmailId = message.Id,
                    Status = SendEmailStatus.Sent,
                    Message = "Email sent successfully",
                };
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Exception during immediate send for message {MessageId}, queuing for Mail retry",
                    message.Id
                );
                message.MarkAsQueued();
                await messageRepository.UpdateAsync(message, ct);
                await hubContext.Clients.All.SendAsync("EmailQueued", ct);

                return new SendEmailResponse
                {
                    EmailId = message.Id,
                    Status = SendEmailStatus.Accepted,
                    Message = "Email accepted and queued for retry after exception",
                };
            }
        }

        logger.LogInformation(
            "Scheduling email to {To} for delivery at {ScheduledFor}",
            message.To,
            message.ScheduledFor
        );

        message.MarkAsQueued();
        await messageRepository.AddAsync(message, ct);
        await hubContext.Clients.All.SendAsync("EmailQueued", ct);

        return new SendEmailResponse
        {
            EmailId = message.Id,
            Status = SendEmailStatus.Scheduled,
            Message = $"Email scheduled for delivery at {message.ScheduledFor.Value:u} (UTC)",
        };
    }
}
