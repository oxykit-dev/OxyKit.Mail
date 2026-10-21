namespace OxyKit.Mail.Contracts;

/// <summary>Body for <c>POST /api/v1/email/send</c>.</summary>
public sealed class SendEmailRequest
{
    public string To { get; set; } = "";
    public string Cc { get; set; } = "";
    public string Bcc { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
    public bool IsHtml { get; set; }
    public IList<AttachmentRequest>? Attachments { get; set; }
    public DateTime? ScheduledFor { get; set; }
}
