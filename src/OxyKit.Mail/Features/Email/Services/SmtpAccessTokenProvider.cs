using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.Email.Configuration;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Acquires SMTP OAuth2 access tokens for Microsoft 365 and Google (config-only).
/// Caches tokens until shortly before expiry.
/// </summary>
public sealed class SmtpAccessTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<SmtpSettings> smtpSettings,
    ILogger<SmtpAccessTokenProvider> logger
) : ISmtpAccessTokenProvider
{
    public const string HttpClientName = "SmtpOAuth2";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cachedAccessToken;
    private DateTimeOffset _expiresAt = DateTimeOffset.MinValue;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (TryGetCachedToken(out cached))
            {
                return cached;
            }

            var settings = smtpSettings.CurrentValue;
            if (settings.AuthMode != SmtpAuthMode.OAuth2)
            {
                throw new InvalidOperationException(
                    "SMTP OAuth2 token provider was called while AuthMode is not OAuth2."
                );
            }

            var oauth =
                settings.OAuth2
                ?? throw new InvalidOperationException("SmtpSettings:OAuth2 is not configured.");

            var token = await RequestTokenAsync(oauth, cancellationToken).ConfigureAwait(false);
            _cachedAccessToken = token.AccessToken;
            // Refresh two minutes early to avoid edge-of-expiry SMTP failures.
            var lifetime = Math.Max(60, token.ExpiresIn - 120);
            _expiresAt = DateTimeOffset.UtcNow.AddSeconds(lifetime);

            logger.LogDebug(
                "Acquired {Provider} SMTP access token ({GrantType}); caches until {ExpiresAt:u}.",
                oauth.Provider,
                oauth.GrantType,
                _expiresAt
            );

            return _cachedAccessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool TryGetCachedToken(out string token)
    {
        if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTimeOffset.UtcNow < _expiresAt)
        {
            token = _cachedAccessToken;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private Task<OAuth2TokenResponse> RequestTokenAsync(
        SmtpOAuth2Settings oauth,
        CancellationToken cancellationToken
    )
    {
        var endpoint = SmtpOAuth2Defaults.ResolveTokenEndpoint(oauth);
        var scope = SmtpOAuth2Defaults.ResolveScope(oauth);
        var client = httpClientFactory.CreateClient(HttpClientName);
        var label = $"{oauth.Provider}/{oauth.GrantType}";

        IEnumerable<KeyValuePair<string, string>> form = oauth.GrantType switch
        {
            SmtpOAuth2GrantType.RefreshToken => new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = oauth.ClientId,
                ["client_secret"] = oauth.ClientSecret,
                ["refresh_token"] = oauth.RefreshToken,
                ["scope"] = scope,
            },
            SmtpOAuth2GrantType.ClientCredentials => new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = oauth.ClientId,
                ["client_secret"] = oauth.ClientSecret,
                ["scope"] = scope,
            },
            _ => throw new InvalidOperationException(
                $"SMTP OAuth2 grant '{oauth.GrantType}' is not supported."
            ),
        };

        return OAuth2TokenHttp.RequestTokenAsync(
            client,
            endpoint,
            form,
            label,
            logger,
            cancellationToken
        );
    }
}
