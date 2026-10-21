namespace OxyKit.Mail.Contracts;

/// <summary>Attachment for <see cref="SendEmailRequest"/> (JSON encodes <see cref="Content"/> as base64).</summary>
public sealed class AttachmentRequest
{
    public string FileName { get; set; } = "";
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "";
}
