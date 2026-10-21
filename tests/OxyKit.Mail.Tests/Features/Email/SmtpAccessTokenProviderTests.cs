using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Features.Email.Services;

namespace OxyKit.Mail.Tests.Features.Email;

public class SmtpAccessTokenProviderTests
{
    [Fact]
    public async Task GetAccessTokenAsync_MicrosoftRefreshToken_RequestsAndCaches()
    {
        var requestCount = 0;
        string? requestUri = null;
        string? body = null;
        var handler = CreateHandler(
            () =>
            {
                requestCount++;
                return OkToken("tok-ms");
            },
            async req =>
            {
                requestUri = req.RequestUri!.ToString();
                body = await req.Content!.ReadAsStringAsync();
            }
        );

        var sut = CreateSut(
            handler,
            new SmtpSettings
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
            }
        );

        var first = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);
        var second = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        first.Should().Be("tok-ms");
        second.Should().Be("tok-ms");
        requestCount.Should().Be(1);
        requestUri.Should().Be("https://login.microsoftonline.com/tenant-id/oauth2/v2.0/token");
        body.Should().Contain("grant_type=refresh_token");
        body.Should().Contain("refresh_token=refresh-token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_GoogleRefreshToken_UsesGoogleEndpoint()
    {
        string? requestUri = null;
        var handler = CreateHandler(
            () => OkToken("tok-google"),
            req =>
            {
                requestUri = req.RequestUri!.ToString();
                return Task.CompletedTask;
            }
        );

        var sut = CreateSut(
            handler,
            new SmtpSettings
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
            }
        );

        var token = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        token.Should().Be("tok-google");
        requestUri.Should().Be("https://oauth2.googleapis.com/token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_MicrosoftClientCredentials_UsesClientCredentialsGrant()
    {
        string? body = null;
        var handler = CreateHandler(
            () => OkToken("tok-cc"),
            async req =>
            {
                body = await req.Content!.ReadAsStringAsync();
            }
        );

        var sut = CreateSut(
            handler,
            new SmtpSettings
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
            }
        );

        var token = await sut.GetAccessTokenAsync(TestContext.Current.CancellationToken);

        token.Should().Be("tok-cc");
        body.Should().Contain("grant_type=client_credentials");
        body.Should().NotContain("refresh_token");
    }

    [Fact]
    public async Task GetAccessTokenAsync_WhenTokenEndpointFails_Throws()
    {
        var handler = CreateHandler(() =>
            new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    """{"error":"invalid_client"}""",
                    Encoding.UTF8,
                    "application/json"
                ),
            }
        );

        var sut = CreateSut(
            handler,
            new SmtpSettings
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
                    ClientSecret = "bad-secret",
                    RefreshToken = "refresh-token",
                },
            }
        );

        var act = async () => await sut.GetAccessTokenAsync();

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to acquire*SMTP access token*");
    }

    [Fact]
    public void ResolveScope_UsesProviderDefaults()
    {
        SmtpOAuth2Defaults
            .ResolveScope(
                new SmtpOAuth2Settings
                {
                    Provider = SmtpOAuth2Provider.Microsoft,
                    GrantType = SmtpOAuth2GrantType.RefreshToken,
                }
            )
            .Should()
            .Be(SmtpOAuth2Defaults.MicrosoftRefreshTokenScope);

        SmtpOAuth2Defaults
            .ResolveScope(
                new SmtpOAuth2Settings
                {
                    Provider = SmtpOAuth2Provider.Microsoft,
                    GrantType = SmtpOAuth2GrantType.ClientCredentials,
                }
            )
            .Should()
            .Be(SmtpOAuth2Defaults.MicrosoftClientCredentialsScope);

        SmtpOAuth2Defaults
            .ResolveScope(
                new SmtpOAuth2Settings
                {
                    Provider = SmtpOAuth2Provider.Google,
                    GrantType = SmtpOAuth2GrantType.RefreshToken,
                }
            )
            .Should()
            .Be(SmtpOAuth2Defaults.GoogleRefreshTokenScope);
    }

    private static Mock<HttpMessageHandler> CreateHandler(
        Func<HttpResponseMessage> responseFactory,
        Func<HttpRequestMessage, Task>? onRequest = null
    )
    {
        var handler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .Returns(
                async (HttpRequestMessage req, CancellationToken _) =>
                {
                    if (onRequest is not null)
                    {
                        await onRequest(req);
                    }

                    return responseFactory();
                }
            );
        return handler;
    }

    private static HttpResponseMessage OkToken(string accessToken) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"{{accessToken}}","expires_in":3600,"token_type":"Bearer"}""",
                Encoding.UTF8,
                "application/json"
            ),
        };

    private static SmtpAccessTokenProvider CreateSut(
        Mock<HttpMessageHandler> handler,
        SmtpSettings settings
    )
    {
        var factory = new Mock<IHttpClientFactory>();
        factory
            .Setup(f => f.CreateClient(SmtpAccessTokenProvider.HttpClientName))
            .Returns(new HttpClient(handler.Object));

        var monitor = new Mock<IOptionsMonitor<SmtpSettings>>();
        monitor.Setup(m => m.CurrentValue).Returns(settings);

        return new SmtpAccessTokenProvider(
            factory.Object,
            monitor.Object,
            NullLogger<SmtpAccessTokenProvider>.Instance
        );
    }
}
