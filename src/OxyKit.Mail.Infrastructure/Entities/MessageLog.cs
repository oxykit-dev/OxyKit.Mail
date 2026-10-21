namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Log entry for message processing events.
/// </summary>
public class MessageLog : EntityBase
{
    public Guid MessageId { get; set; }
    public Message Message { get; set; } = null!;
    public MessageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}
