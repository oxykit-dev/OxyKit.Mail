using System.Text.Json.Serialization;

namespace OxyKit.Mail.Features.Email.Services;

internal sealed record OAuth2TokenResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken
);
