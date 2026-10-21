namespace OxyKit.Mail.Features.Dashboard;

public record MessageLogResponse(
    Guid Id,
    Guid MessageId,
    string Subject,
    string To,
    string From,
    string? ErrorMessage,
    string Status,
    DateTime LoggedAt
);
