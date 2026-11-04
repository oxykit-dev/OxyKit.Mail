using System.Net;
using System.Text.Json;
using OxyKit.Mail.Tests.Infrastructure;

namespace OxyKit.Mail.Tests.Integration;

public class OpenApiDocumentTests : IDisposable
{
    private readonly OxyKitMailWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OpenApiDocumentTests()
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
    public async Task OpenApi_v1_is_anonymous_and_includes_core_routes()
    {
        var ct = TestContext.Current.CancellationToken;
        var response = await _client.GetAsync("/openapi/v1.json", ct);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;

        root.GetProperty("info").GetProperty("title").GetString().Should().Be("OxyKit.Mail API");
        root.GetProperty("info").GetProperty("version").GetString().Should().Be("v1");

        var pathKeys = root
            .GetProperty("paths")
            .EnumerateObject()
            .Select(p => p.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        pathKeys.Should().Contain(k => k.EndsWith("/email/send", StringComparison.Ordinal));
        pathKeys
            .Should()
            .Contain(
                k => k.EndsWith("/email/dkim/generate-keys", StringComparison.Ordinal),
                because: $"paths were: {string.Join(", ", pathKeys)}"
            );
        pathKeys
            .Should()
            .Contain(
                k => k.EndsWith("/email/dkim/dns-record", StringComparison.Ordinal),
                because: $"paths were: {string.Join(", ", pathKeys)}"
            );
        pathKeys
            .Should()
            .Contain(k => k.EndsWith("/dashboard/email-stats", StringComparison.Ordinal));

        // Optional: write companion snapshot when regenerating.
        // OXYKIT_EXPORT_OPENAPI=/abs/path/oxykit-mail-api.json
        var exportPath = Environment.GetEnvironmentVariable("OXYKIT_EXPORT_OPENAPI");
        if (!string.IsNullOrWhiteSpace(exportPath))
        {
            var json = JsonSerializer.Serialize(
                root,
                new JsonSerializerOptions { WriteIndented = true }
            );
            await File.WriteAllTextAsync(exportPath, json + "\n", ct);
        }
    }
}
