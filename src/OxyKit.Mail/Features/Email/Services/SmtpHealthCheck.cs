using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.Email.Configuration;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Probes SMTP connectivity with a short timeout so /health cannot hang.
/// </summary>
public class SmtpHealthCheck(IOptions<SmtpSettings> options, ILogger<SmtpHealthCheck> logger)
    : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    private readonly SmtpSettings _settings = options.Value;
    private readonly ILogger<SmtpHealthCheck> _logger = logger;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );
            timeoutCts.CancelAfter(ProbeTimeout);

            using var client = new SmtpClient();
            await client
                .ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None,
                    timeoutCts.Token
                )
                .ConfigureAwait(false);
            await client.DisconnectAsync(true, timeoutCts.Token).ConfigureAwait(false);
            return HealthCheckResult.Healthy("SMTP server is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP health check failed");
            return HealthCheckResult.Unhealthy($"SMTP health check failed: {ex.Message}");
        }
    }
}
