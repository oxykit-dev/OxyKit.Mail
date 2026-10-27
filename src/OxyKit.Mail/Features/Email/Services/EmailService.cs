using ErrorOr;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OxyKit.Mail.Contracts;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;
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
    /// Optional <paramref name="idempotencyKey"/> returns the existing acceptance when set.
    /// </summary>
    Task<ErrorOr<SendEmailResponse>> AcceptAsync(
        SendEmailRequest request,
        string? idempotencyKey = null,
        CancellationToken ct = default
    );
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
        string? idempotencyKey = null,
        CancellationToken ct = default
    )
    {
        var key = NormalizeIdempotencyKey(idempotencyKey);
        if (key is not null)
        {
            if (key.Length > 128)
                return Error.Validation(description: "Idempotency-Key must be at most 128 characters.");

            var existing = await messageRepository.GetByIdempotencyKeyAsync(key, ct);
            if (existing is not null)
                return ToResponse(existing);
        }

        var message = EmailMapper.MapFromRequest(request, _smtpSettings);
        message.IdempotencyKey = key;
        message.ThrowIfInvalid();

        if (message.ScheduledFor == null)
        {
            logger.LogInformation(
                "Sending email immediately to {To} with subject: {Subject}",
                message.To,
                message.Subject
            );

            var addResult = await TryAddAsync(message, key, ct);
            if (addResult is not null)
                return addResult;

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
        var scheduledAdd = await TryAddAsync(message, key, ct);
        if (scheduledAdd is not null)
            return scheduledAdd;

        await hubContext.Clients.All.SendAsync("EmailQueued", ct);

        return new SendEmailResponse
        {
            EmailId = message.Id,
            Status = SendEmailStatus.Scheduled,
            Message = $"Email scheduled for delivery at {message.ScheduledFor.Value:u} (UTC)",
        };
    }

    /// <summary>
    /// Adds the message; on unique idempotency race, returns the existing acceptance.
    /// </summary>
    private async Task<SendEmailResponse?> TryAddAsync(
        Message message,
        string? key,
        CancellationToken ct
    )
    {
        try
        {
            await messageRepository.AddAsync(message, ct);
            return null;
        }
        catch (DbUpdateException ex)
            when (key is not null && ex.IsUniqueConstraintViolation())
        {
            var raced = await messageRepository.GetByIdempotencyKeyAsync(key, ct);
            if (raced is not null)
                return ToResponse(raced);
            throw;
        }
    }

    private static string? NormalizeIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return null;
        return idempotencyKey.Trim();
    }

    internal static SendEmailResponse ToResponse(Message message)
    {
        if (message.Status == MessageStatus.Sent)
        {
            return new SendEmailResponse
            {
                EmailId = message.Id,
                Status = SendEmailStatus.Sent,
                Message = "Email sent successfully",
            };
        }

        // Only report scheduled while still in the future; due/queued/processing → accepted.
        if (message.ScheduledFor is { } scheduled && scheduled > DateTime.UtcNow)
        {
            return new SendEmailResponse
            {
                EmailId = message.Id,
                Status = SendEmailStatus.Scheduled,
                Message = $"Email scheduled for delivery at {scheduled:u} (UTC)",
            };
        }

        return new SendEmailResponse
        {
            EmailId = message.Id,
            Status = SendEmailStatus.Accepted,
            Message = "Email previously accepted for this Idempotency-Key",
        };
    }
}
