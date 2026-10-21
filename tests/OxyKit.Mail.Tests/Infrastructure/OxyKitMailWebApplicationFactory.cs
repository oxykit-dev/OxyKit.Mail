using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace OxyKit.Mail.Tests.Infrastructure;

/// <summary>
/// Host factory for HTTP smoke / integration tests.
/// Program registers services; this only overrides config and environment.
/// </summary>
public class OxyKitMailWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["DatabaseProvider"] = "InMemory",
                        ["Jwt:Key"] = "ThisIsATestKeyForJwtTokenGenerationWithAtLeast32Characters!",
                        ["Jwt:Issuer"] = "OxyKit.Mail.Tests",
                        ["Jwt:Audience"] = "OxyKit.Mail.Tests",
                        ["Jwt:ExpiresInMinutes"] = "60",
                        ["SmtpSettings:Host"] = "localhost",
                        ["SmtpSettings:Port"] = "25",
                        ["SmtpSettings:Username"] = "test@test.com",
                        ["SmtpSettings:Password"] = "test-password-long",
                        ["SmtpSettings:FromAddress"] = "test@test.com",
                        ["SmtpSettings:FromName"] = "Test",
                        ["SmtpSettings:EnableSsl"] = "false",
                        ["Cors:AllowedOrigins:0"] = "http://localhost:5173",
                        ["Cors:AllowCredentials"] = "true",
                        ["RateLimit:Auth:PermitLimit"] = "1000",
                        ["RateLimit:Auth:WindowSeconds"] = "60",
                        ["RateLimit:Auth:QueueLimit"] = "100",
                        ["RateLimit:Api:PermitLimit"] = "1000",
                        ["RateLimit:Api:WindowSeconds"] = "60",
                        ["RateLimit:Api:QueueLimit"] = "100",
                        ["DkimSettings:Enabled"] = "false",
                    }
                );
            }
        );
    }
}
