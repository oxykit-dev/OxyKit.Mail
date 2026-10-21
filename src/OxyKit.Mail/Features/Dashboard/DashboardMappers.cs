using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Mappers for converting dashboard entities to response DTOs.
/// </summary>
public static class DashboardMappers
{
    /// <summary>
    /// Converts a MessageLog entity to a MessageLogResponse DTO.
    /// </summary>
    /// <param name="log">The message log entity.</param>
    /// <returns>A MessageLogResponse DTO.</returns>
    public static MessageLogResponse ToMessageLogResponse(MessageLog log)
    {
        return new MessageLogResponse(
            log.Id,
            log.MessageId,
            log.Message.Subject,
            log.Message.To,
            log.Message.From,
            log.ErrorMessage,
            log.Status.ToString(),
            log.LoggedAt
        );
    }

    /// <summary>
    /// Converts a dead-lettered Message entity to a DeadLetteredEmailResponse DTO.
    /// </summary>
    /// <param name="message">The dead-lettered message entity.</param>
    /// <returns>A DeadLetteredEmailResponse DTO.</returns>
    public static DeadLetteredEmailResponse ToDeadLetteredEmailResponse(Message message)
    {
        return new DeadLetteredEmailResponse(
            message.Id,
            message.From,
            message.To,
            message.Subject,
            message.CreatedAt,
            message.DeadLetterReason ?? string.Empty
        );
    }
}
