using System.Text.Json;

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

    /// <summary>
    /// Optional local mail template id (folder under MailTemplates:TemplatesRoot).
    /// When set, <see cref="Subject"/> / <see cref="Body"/> are rendered from the pack
    /// (<c>subject.txt</c> + <c>body.html</c>) using <see cref="Data"/>; <see cref="IsHtml"/> becomes true.
    /// </summary>
    public string? TemplateId { get; set; }

    /// <summary>JSON object merged into the mail template (required when <see cref="TemplateId"/> is set).</summary>
    public JsonElement? Data { get; set; }
}
