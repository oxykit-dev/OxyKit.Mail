namespace OxyKit.Mail.Infrastructure;

/// <summary>
/// Identity roles. <see cref="Sender"/> is send-only (POST email/send, Me, and own password);
/// dashboard / users / DKIM admin require <see cref="Admin"/> or <see cref="User"/>.
/// </summary>
public static class AppRoles
{
    public const string Admin = "Admin";
    public const string User = "User";
    public const string Sender = "Sender";
}
