namespace OxyKit.Mail.Infrastructure.Entities;

/// <summary>
/// Represents a refresh token for user authentication.
/// </summary>
public class RefreshToken : EntityBase
{
    public string Token { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool Revoked { get; set; } = false;
}
