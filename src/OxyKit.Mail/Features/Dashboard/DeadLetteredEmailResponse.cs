namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Response containing dead-lettered email information.
/// </summary>
public record DeadLetteredEmailResponse(
    Guid Id,
    string From,
    string To,
    string Subject,
    DateTime CreatedAt,
    string DeadLetterReason
);
