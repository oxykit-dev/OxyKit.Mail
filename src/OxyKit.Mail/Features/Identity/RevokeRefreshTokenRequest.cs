namespace OxyKit.Mail.Features.Identity;

public record RevokeRefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
    public string DeviceId { get; init; } = string.Empty;
}
