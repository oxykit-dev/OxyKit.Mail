namespace OxyKit.Mail.Features.Identity;

public record RefreshTokenResponse
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
}
