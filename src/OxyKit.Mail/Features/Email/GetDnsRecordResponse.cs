namespace OxyKit.Mail.Features.Email;

/// <summary>
/// Response containing DKIM DNS TXT record.
/// </summary>
/// <param name="Success">Whether the operation succeeded.</param>
/// <param name="DnsTxtRecord">The DNS TXT record content.</param>
/// <param name="Instructions">Instructions for adding the DNS record.</param>
/// <param name="Message">Success or error message.</param>
public record GetDnsRecordResponse(
    bool Success,
    string DnsTxtRecord,
    string Instructions,
    string Message
);
