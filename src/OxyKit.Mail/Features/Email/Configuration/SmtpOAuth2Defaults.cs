namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// Default scopes and token endpoints for SMTP OAuth2 providers.
/// </summary>
public static class SmtpOAuth2Defaults
{
    public const string MicrosoftRefreshTokenScope =
        "https://outlook.office365.com/SMTP.Send offline_access";

    public const string MicrosoftClientCredentialsScope = "https://outlook.office365.com/.default";

    public const string GoogleRefreshTokenScope = "https://mail.google.com/";

    public const string GoogleTokenEndpoint = "https://oauth2.googleapis.com/token";

    public static string ResolveScope(SmtpOAuth2Settings oauth)
    {
        if (!string.IsNullOrWhiteSpace(oauth.Scope))
        {
            return oauth.Scope;
        }

        return (oauth.Provider, oauth.GrantType) switch
        {
            (SmtpOAuth2Provider.Microsoft, SmtpOAuth2GrantType.ClientCredentials) =>
                MicrosoftClientCredentialsScope,
            (SmtpOAuth2Provider.Microsoft, _) => MicrosoftRefreshTokenScope,
            (SmtpOAuth2Provider.Google, _) => GoogleRefreshTokenScope,
            _ => throw new InvalidOperationException(
                $"No default scope for provider '{oauth.Provider}'."
            ),
        };
    }

    public static string ResolveTokenEndpoint(SmtpOAuth2Settings oauth)
    {
        if (!string.IsNullOrWhiteSpace(oauth.TokenEndpoint))
        {
            return oauth.TokenEndpoint;
        }

        return oauth.Provider switch
        {
            SmtpOAuth2Provider.Microsoft =>
                $"{oauth.Authority.TrimEnd('/')}/{oauth.TenantId}/oauth2/v2.0/token",
            SmtpOAuth2Provider.Google => GoogleTokenEndpoint,
            _ => throw new InvalidOperationException(
                $"No default token endpoint for provider '{oauth.Provider}'."
            ),
        };
    }
}
