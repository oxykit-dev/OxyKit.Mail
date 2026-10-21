namespace OxyKit.Mail.Features.Dashboard;

/// <summary>
/// Response containing top recipient information.
/// </summary>
public record TopRecipientResponse(string Recipient, int EmailsReceived);
