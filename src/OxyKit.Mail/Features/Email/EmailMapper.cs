using MimeKit;
using OxyKit.Mail.Contracts;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Mapper for converting between email domain models and external representations.
/// </summary>
public static class EmailMapper
{
    /// <summary>
    /// Converts a Message domain entity to a MimeMessage for SMTP sending.
    /// </summary>
    /// <param name="emailMessage">The domain message entity.</param>
    /// <returns>A MimeMessage ready to be sent via SMTP.</returns>
    public static MimeMessage ToMimeMessage(Message emailMessage)
    {
        var mimeMessage = new MimeMessage();

        mimeMessage.From.Add(new MailboxAddress("", emailMessage.From));

        mimeMessage.To.AddRange(
            emailMessage.To.Split(',').Select(email => new MailboxAddress("", email.Trim()))
        );

        if (!string.IsNullOrEmpty(emailMessage.Cc))
        {
            mimeMessage.Cc.AddRange(
                emailMessage.Cc.Split(',').Select(email => new MailboxAddress("", email.Trim()))
            );
        }

        if (!string.IsNullOrEmpty(emailMessage.Bcc))
        {
            mimeMessage.Bcc.AddRange(
                emailMessage.Bcc.Split(',').Select(email => new MailboxAddress("", email.Trim()))
            );
        }

        mimeMessage.Subject = emailMessage.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = emailMessage.IsHtml ? emailMessage.Body : null,
            TextBody = !emailMessage.IsHtml ? emailMessage.Body : null,
        };

        foreach (var attachment in emailMessage.Attachments)
        {
            bodyBuilder.Attachments.Add(
                attachment.FileName,
                attachment.Content,
                ContentType.Parse(attachment.ContentType)
            );
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    /// <summary>
    /// Maps a send request to a Message entity.
    /// </summary>
    public static Message MapFromRequest(SendEmailRequest request, SmtpSettings smtpSettings) =>
        new()
        {
            From = smtpSettings.UserName,
            To = request.To,
            Cc = request.Cc,
            Bcc = request.Bcc,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            Attachments =
                request
                    .Attachments?.Where(a =>
                        !string.IsNullOrEmpty(a.FileName)
                        && a.Content != null
                        && !string.IsNullOrEmpty(a.ContentType)
                    )
                    .Select(a =>
                    {
                        var attachment = new Attachment
                        {
                            FileName = a.FileName,
                            Content = a.Content,
                            ContentType = a.ContentType,
                        };
                        attachment.ThrowIfInvalid();
                        return attachment;
                    })
                    .ToList()
                ?? [],
            CreatedAt = DateTime.UtcNow,
            ScheduledFor = request.ScheduledFor,
        };
}
