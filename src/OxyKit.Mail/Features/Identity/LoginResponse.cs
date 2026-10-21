namespace OxyKit.Mail.Features.Identity;

public record LoginResponse
{
    public string Token { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}
