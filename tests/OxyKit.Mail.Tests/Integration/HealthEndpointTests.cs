using System.Net;
using FluentAssertions;
using OxyKit.Mail.Tests.Infrastructure;

namespace OxyKit.Mail.Tests.Integration;

public class HealthEndpointTests : IDisposable
{
    private readonly OxyKitMailWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointTests()
    {
        _factory = new OxyKitMailWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Health_ReturnsSuccessOrDegraded_NotServerError()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        // SMTP probe may be Unhealthy (503) against localhost:25; host must still boot.
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
    }
}
