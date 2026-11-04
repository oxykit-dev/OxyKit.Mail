using System.Net;
using FluentAssertions;
using OxyKit.Mail.Tests.Infrastructure;

namespace OxyKit.Mail.Tests.Integration;

public class CorrelationIdTests : IDisposable
{
    private readonly OxyKitMailWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CorrelationIdTests()
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
    public async Task Health_WithoutCorrelationHeader_GeneratesAndEchoes()
    {
        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Single().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Health_WithCorrelationHeader_EchoesSameValue()
    {
        const string expected = "mail-corr-test-001";
        _client.DefaultRequestHeaders.Add("X-Correlation-Id", expected);

        var response = await _client.GetAsync("/health", TestContext.Current.CancellationToken);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values!.Single().Should().Be(expected);
    }
}
