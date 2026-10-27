using System.Net;
using System.Text;
using OxyKit.Mail.Client;
using OxyKit.Mail.Contracts;

namespace OxyKit.Mail.Client.Tests;

public class MailClientTests
{
    [Fact]
    public async Task SendAsync_success_accepted_returns_result()
    {
        var handler = new StubHandler(
            HttpStatusCode.Accepted,
            """{"emailId":"11111111-1111-1111-1111-111111111111","status":"accepted","message":"queued"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        var result = await client.SendAsync(
            new SendEmailRequest
            {
                To = "a@b.com",
                Subject = "s",
                Body = "b",
                IsHtml = false,
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(SendEmailStatus.Accepted, result.Status);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), result.EmailId);
        Assert.Equal(202, result.HttpStatusCode);
        Assert.Equal("api/v1/email/send", handler.LastPath);
    }

    [Fact]
    public async Task SendAsync_success_sent_returns_result()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"emailId":"22222222-2222-2222-2222-222222222222","status":"sent","message":"Email sent successfully"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        var result = await client.SendAsync(
            new SendEmailRequest
            {
                To = "a@b.com",
                Subject = "s",
                Body = "b",
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(SendEmailStatus.Sent, result.Status);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), result.EmailId);
        Assert.Equal(200, result.HttpStatusCode);
    }

    [Fact]
    public async Task SendAsync_success_scheduled_returns_result()
    {
        var handler = new StubHandler(
            HttpStatusCode.Accepted,
            """{"emailId":"33333333-3333-3333-3333-333333333333","status":"scheduled","message":"scheduled"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        var result = await client.SendAsync(
            new SendEmailRequest
            {
                To = "a@b.com",
                Subject = "s",
                Body = "b",
                ScheduledFor = DateTime.UtcNow.AddHours(1),
            },
            TestContext.Current.CancellationToken
        );

        Assert.Equal(SendEmailStatus.Scheduled, result.Status);
        Assert.Equal(202, result.HttpStatusCode);
    }

    [Fact]
    public async Task SendAsync_success_without_emailId_throws()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"emailId":"00000000-0000-0000-0000-000000000000","status":"sent","message":"ok"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        await Assert.ThrowsAsync<MailApiException>(() =>
            client.SendAsync(
                new SendEmailRequest
                {
                    To = "a@b.com",
                    Subject = "s",
                    Body = "b",
                },
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task SendAsync_success_with_unknown_status_throws()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"emailId":"11111111-1111-1111-1111-111111111111","status":"queued","message":"weird"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        // Unknown enum member fails during JSON deserialize (before MailApiException wrapping).
        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            client.SendAsync(
                new SendEmailRequest
                {
                    To = "a@b.com",
                    Subject = "s",
                    Body = "b",
                },
                TestContext.Current.CancellationToken
            )
        );
    }

    [Fact]
    public async Task SendAsync_error_status_throws_MailApiException()
    {
        var handler = new StubHandler(
            HttpStatusCode.Unauthorized,
            """{"message":"missing key"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        var ex = await Assert.ThrowsAsync<MailApiException>(() =>
            client.SendAsync(
                new SendEmailRequest
                {
                    To = "a@b.com",
                    Subject = "s",
                    Body = "b",
                },
                TestContext.Current.CancellationToken
            )
        );
        Assert.Equal(401, ex.StatusCode);
        Assert.Equal("missing key", ex.Error);
    }

    [Fact]
    public void Options_ctor_sets_api_key_header()
    {
        using var client = new MailClient(
            new MailClientOptions
            {
                BaseAddress = new Uri("http://localhost:5026/"),
                ApiKey = "test-key",
            }
        );
        Assert.NotNull(client);
    }

    [Fact]
    public async Task SendAsync_with_idempotency_key_sets_header()
    {
        var handler = new StubHandler(
            HttpStatusCode.OK,
            """{"emailId":"22222222-2222-2222-2222-222222222222","status":"sent","message":"ok"}"""
        );
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5026/"),
        };
        using var client = new MailClient(http);

        await client.SendAsync(
            new SendEmailRequest
            {
                To = "a@b.com",
                Subject = "s",
                Body = "b",
            },
            "job-id:0",
            TestContext.Current.CancellationToken
        );

        Assert.Equal("job-id:0", handler.LastIdempotencyKey);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? LastPath { get; private set; }
        public string? LastIdempotencyKey { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken
        )
        {
            if (request.RequestUri is not null)
                LastPath = request.RequestUri.PathAndQuery.TrimStart('/');
            if (request.Headers.TryGetValues("Idempotency-Key", out var values))
                LastIdempotencyKey = values.FirstOrDefault();

            return Task.FromResult(
                new HttpResponseMessage(status)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                }
            );
        }
    }
}
