using System.Text.Json;

namespace OxyKit.Mail.Features.Email.Services;

internal static class OAuth2TokenHttp
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<OAuth2TokenResponse> RequestTokenAsync(
        HttpClient client,
        string tokenEndpoint,
        IEnumerable<KeyValuePair<string, string>> formFields,
        string providerLabel,
        ILogger logger,
        CancellationToken cancellationToken
    )
    {
        using var content = new FormUrlEncodedContent(formFields);
        using var response = await client
            .PostAsync(tokenEndpoint, content, cancellationToken)
            .ConfigureAwait(false);

        var body = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "{Provider} token request failed with {StatusCode}: {Body}",
                providerLabel,
                (int)response.StatusCode,
                Truncate(body)
            );
            throw new InvalidOperationException(
                $"Failed to acquire {providerLabel} SMTP access token ({(int)response.StatusCode})."
            );
        }

        var token =
            JsonSerializer.Deserialize<OAuth2TokenResponse>(body, JsonOptions)
            ?? throw new InvalidOperationException(
                $"{providerLabel} token response could not be deserialized."
            );

        if (string.IsNullOrWhiteSpace(token.AccessToken))
        {
            throw new InvalidOperationException(
                $"{providerLabel} token response did not include an access_token."
            );
        }

        return token.ExpiresIn <= 0 ? token with { ExpiresIn = 3600 } : token;
    }

    private static string Truncate(string value, int max = 256) =>
        value.Length <= max ? value : value[..max] + "…";
}
