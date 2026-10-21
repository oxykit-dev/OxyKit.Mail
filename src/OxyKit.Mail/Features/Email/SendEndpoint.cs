using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Contracts;
using OxyKit.Mail.Features.Email.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Accepts email for Mail-owned delivery (supports scheduling).
/// Implements rate limiting to prevent spam and abuse.
/// </summary>
public class SendEndpoint(IEmailService emailService)
    : MailEndpoint<SendEmailRequest, SendEmailResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Post("/email/send");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.Sender);
        Validator<SendEmailRequestValidator>();
        AuthSchemes(AuthSchemeNames.Bearer, AuthSchemeNames.ApiKey);

        Options(x => x.RequireRateLimiting(RateLimitingExtensions.EmailPolicy));

        Summary(s =>
        {
            s.Summary =
                "Accepts an email for send or schedule; returns delivery acceptance status.";
            s.Description =
                "Attempts immediate SMTP delivery when 'scheduledFor' is omitted. "
                + "On SMTP failure the message is queued for Mail-side retry and status is 'accepted' (HTTP 202). "
                + "When 'scheduledFor' is set, status is 'scheduled' (HTTP 202). "
                + "Immediate SMTP success returns status 'sent' (HTTP 200). "
                + "Callers (e.g. OxyKit.Flow) must treat sent/accepted/scheduled as success and must not re-send. "
                + "Rate limited to prevent spam (50 requests per minute by default).";
            s.ResponseExamples[200] = new SendEmailResponse
            {
                EmailId = Guid.Empty,
                Status = SendEmailStatus.Sent,
                Message = "Email sent successfully",
            };
            s.ResponseExamples[202] = new SendEmailResponse
            {
                EmailId = Guid.Empty,
                Status = SendEmailStatus.Accepted,
                Message = "Email accepted and queued for retry after initial send failure",
            };
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SendEmailRequest req, CancellationToken ct)
    {
        if (ValidationFailed)
        {
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var result = await emailService.AcceptAsync(req, ct);
        if (await SendOnErrorAsync(result, ct))
            return;

        var response = result.Value;
        var statusCode = response.Status switch
        {
            SendEmailStatus.Sent => StatusCodes.Status200OK,
            _ => StatusCodes.Status202Accepted,
        };

        await Send.ResponseAsync(response, statusCode, ct);
    }
}
