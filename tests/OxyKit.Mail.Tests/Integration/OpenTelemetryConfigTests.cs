using FluentAssertions;
using OxyKit.Mail.Tests.Infrastructure;

namespace OxyKit.Mail.Tests.Integration;

public class OpenTelemetryConfigTests
{
    [Fact]
    public async Task Host_WithOpenTelemetryEnabled_AndBogusEndpoint_StillStarts()
    {
        Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://127.0.0.1:9");
        try
        {
            using var factory = new OxyKitMailWebApplicationFactory().WithWebHostBuilder(builder =>
            {
                builder.UseSetting("OpenTelemetry:Enabled", "true");
                builder.UseSetting("OpenTelemetry:ServiceName", "OxyKit.Mail.Tests");
            });
            using var client = factory.CreateClient();
            var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);
            response.Should().NotBeNull();
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
        }
    }
}
