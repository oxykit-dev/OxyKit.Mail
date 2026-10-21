using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Represents an email attachment.
/// </summary>
public class Attachment : EntityBase
{
    /// <summary>
    /// Name of the attached file.
    /// </summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Binary content of the attachment.
    /// </summary>
    [Required]
    public byte[] Content { get; set; } = [];

    /// <summary>
    /// MIME type of the attachment (e.g., "application/pdf", "image/png").
    /// </summary>
    [Required]
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the parent message.
    /// </summary>
    [ForeignKey("Message")]
    public Guid MessageId { get; set; }

    /// <summary>
    /// Navigation property to the parent message.
    /// </summary>
    public Message Message { get; set; } = null!;

    /// <summary>
    /// Validates that the attachment has all required fields and content.
    /// </summary>
    /// <returns>True if valid, otherwise false.</returns>
    public bool IsValid() =>
        !string.IsNullOrWhiteSpace(FileName)
        && Content is { Length: > 0 }
        && !string.IsNullOrWhiteSpace(ContentType);

    /// <summary>
    /// Validates that the attachment has all required fields and content.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    public void ThrowIfInvalid()
    {
        if (!IsValid())
            throw new ValidationException("Attachment is missing required fields or content.");
    }

    /// <summary>
    /// Gets the size of the attachment in bytes.
    /// </summary>
    /// <returns>Size in bytes.</returns>
    public long GetSize() => Content?.LongLength ?? 0;

    /// <summary>
    /// Checks if the attachment's content type is in the allowed list.
    /// </summary>
    /// <param name="allowedTypes">List of allowed MIME types.</param>
    /// <returns>True if the content type is allowed, otherwise false.</returns>
    public bool IsTypeAllowed(IEnumerable<string> allowedTypes) =>
        allowedTypes?.Contains(ContentType) == true;
}
