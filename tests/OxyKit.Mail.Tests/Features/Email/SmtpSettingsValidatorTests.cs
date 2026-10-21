using FluentAssertions;
using OxyKit.Mail.Features.Email.Configuration;

namespace OxyKit.Mail.Tests.Features.Email;

public class SmtpSettingsValidatorTests
{
    private readonly SmtpSettingsValidator _sut = new();

    [Fact]
    public void Validate_PasswordMode_WithPassword_Succeeds()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            UserName = "user@example.com",
            Password = "secret",
            AuthMode = SmtpAuthMode.Password,
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_PasswordMode_MissingPassword_Fails()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.example.com",
            Port = 587,
            UserName = "user@example.com",
            AuthMode = SmtpAuthMode.Password,
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("Password", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_OAuth2_MicrosoftRefreshToken_Succeeds()
    {
        var settings = ValidMicrosoftRefreshTokenSettings();

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_OAuth2_GoogleRefreshToken_Succeeds()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            UserName = "sender@gmail.com",
            AuthMode = SmtpAuthMode.OAuth2,
            OAuth2 = new SmtpOAuth2Settings
            {
                Provider = SmtpOAuth2Provider.Google,
                GrantType = SmtpOAuth2GrantType.RefreshToken,
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RefreshToken = "refresh-token",
            },
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_OAuth2_MicrosoftClientCredentials_Succeeds()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.office365.com",
            Port = 587,
            UserName = "sender@contoso.com",
            AuthMode = SmtpAuthMode.OAuth2,
            OAuth2 = new SmtpOAuth2Settings
            {
                Provider = SmtpOAuth2Provider.Microsoft,
                GrantType = SmtpOAuth2GrantType.ClientCredentials,
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
            },
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Validate_OAuth2_MissingOAuth2_Fails()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.office365.com",
            Port = 587,
            UserName = "sender@contoso.com",
            AuthMode = SmtpAuthMode.OAuth2,
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("OAuth2", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_OAuth2_RefreshTokenMissing_Fails()
    {
        var settings = ValidMicrosoftRefreshTokenSettings();
        settings = settings with { OAuth2 = settings.OAuth2! with { RefreshToken = "" } };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result.Failures.Should().Contain(f => f.Contains("RefreshToken", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_OAuth2_GoogleClientCredentials_Fails()
    {
        var settings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            UserName = "sender@gmail.com",
            AuthMode = SmtpAuthMode.OAuth2,
            OAuth2 = new SmtpOAuth2Settings
            {
                Provider = SmtpOAuth2Provider.Google,
                GrantType = SmtpOAuth2GrantType.ClientCredentials,
                ClientId = "client-id",
                ClientSecret = "client-secret",
            },
        };

        var result = _sut.Validate(null, settings);

        result.Succeeded.Should().BeFalse();
        result
            .Failures.Should()
            .Contain(f => f.Contains("ClientCredentials", StringComparison.Ordinal));
    }

    private static SmtpSettings ValidMicrosoftRefreshTokenSettings() =>
        new()
        {
            Host = "smtp.office365.com",
            Port = 587,
            UserName = "sender@contoso.com",
            AuthMode = SmtpAuthMode.OAuth2,
            OAuth2 = new SmtpOAuth2Settings
            {
                Provider = SmtpOAuth2Provider.Microsoft,
                GrantType = SmtpOAuth2GrantType.RefreshToken,
                TenantId = "tenant-id",
                ClientId = "client-id",
                ClientSecret = "client-secret",
                RefreshToken = "refresh-token",
            },
        };
}
