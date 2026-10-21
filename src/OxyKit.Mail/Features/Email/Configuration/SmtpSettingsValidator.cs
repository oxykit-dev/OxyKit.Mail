using Microsoft.Extensions.Options;

namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// Conditional validation for SMTP password vs OAuth2 settings.
/// </summary>
public sealed class SmtpSettingsValidator : IValidateOptions<SmtpSettings>
{
    public ValidateOptionsResult Validate(string? name, SmtpSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            failures.Add("SmtpSettings:Host is required.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("SmtpSettings:Port must be between 1 and 65535.");
        }

        if (string.IsNullOrWhiteSpace(options.UserName))
        {
            failures.Add("SmtpSettings:UserName is required.");
        }

        switch (options.AuthMode)
        {
            case SmtpAuthMode.Password:
                if (string.IsNullOrWhiteSpace(options.Password))
                {
                    failures.Add("SmtpSettings:Password is required when AuthMode is Password.");
                }
                break;

            case SmtpAuthMode.OAuth2:
                ValidateOAuth2(options.OAuth2, failures);
                break;

            default:
                failures.Add($"SmtpSettings:AuthMode '{options.AuthMode}' is not supported.");
                break;
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }

    private static void ValidateOAuth2(SmtpOAuth2Settings? oauth, List<string> failures)
    {
        if (oauth is null)
        {
            failures.Add("SmtpSettings:OAuth2 is required when AuthMode is OAuth2.");
            return;
        }

        if (string.IsNullOrWhiteSpace(oauth.ClientId))
        {
            failures.Add("SmtpSettings:OAuth2:ClientId is required.");
        }

        if (string.IsNullOrWhiteSpace(oauth.ClientSecret))
        {
            failures.Add("SmtpSettings:OAuth2:ClientSecret is required.");
        }

        switch (oauth.GrantType)
        {
            case SmtpOAuth2GrantType.RefreshToken:
                if (string.IsNullOrWhiteSpace(oauth.RefreshToken))
                {
                    failures.Add(
                        "SmtpSettings:OAuth2:RefreshToken is required when GrantType is RefreshToken."
                    );
                }
                break;

            case SmtpOAuth2GrantType.ClientCredentials:
                if (oauth.Provider != SmtpOAuth2Provider.Microsoft)
                {
                    failures.Add(
                        "SmtpSettings:OAuth2:GrantType ClientCredentials is only supported for Provider Microsoft."
                    );
                }
                break;

            default:
                failures.Add(
                    $"SmtpSettings:OAuth2:GrantType '{oauth.GrantType}' is not supported."
                );
                break;
        }

        switch (oauth.Provider)
        {
            case SmtpOAuth2Provider.Microsoft:
                if (string.IsNullOrWhiteSpace(oauth.TenantId))
                {
                    failures.Add(
                        "SmtpSettings:OAuth2:TenantId is required for Provider Microsoft."
                    );
                }

                if (
                    string.IsNullOrWhiteSpace(oauth.TokenEndpoint)
                    && string.IsNullOrWhiteSpace(oauth.Authority)
                )
                {
                    failures.Add(
                        "SmtpSettings:OAuth2:Authority is required for Provider Microsoft when TokenEndpoint is not set."
                    );
                }
                break;

            case SmtpOAuth2Provider.Google:
                break;

            default:
                failures.Add($"SmtpSettings:OAuth2:Provider '{oauth.Provider}' is not supported.");
                break;
        }
    }
}
