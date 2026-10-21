using Microsoft.Extensions.Options;

namespace OxyKit.Mail.Features.Email.Configuration;

/// <summary>
/// Requires Domain / Selector / PrivateKeyPath only when DKIM is enabled.
/// </summary>
public sealed class DkimSettingsValidator : IValidateOptions<DkimSettings>
{
    public ValidateOptionsResult Validate(string? name, DkimSettings options)
    {
        if (!options.Enabled)
            return ValidateOptionsResult.Success;

        var failures = new List<string>();
        if (string.IsNullOrWhiteSpace(options.Domain))
            failures.Add("Domain is required when DKIM is enabled.");
        if (string.IsNullOrWhiteSpace(options.Selector))
            failures.Add("Selector is required when DKIM is enabled.");
        if (string.IsNullOrWhiteSpace(options.PrivateKeyPath))
            failures.Add("PrivateKeyPath is required when DKIM is enabled.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
