using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.Email.Configuration;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Background service that periodically processes the email queue.
/// </summary>
public class QueueBackgroundService(
    IServiceProvider serviceProvider,
    IOptions<QueueSettings> queueSettings,
    ILogger<QueueBackgroundService> logger
) : BackgroundService
{
    private readonly QueueSettings _settings = queueSettings.Value;

    /// <summary>
    /// Executes the background task that processes queued emails.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Email Queue Background Service started. Processing every {Interval} seconds with batch size {BatchSize}.",
            _settings.PollIntervalSeconds,
            _settings.BatchSize
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Create a scope to resolve scoped services
                using var scope = serviceProvider.CreateScope();
                var queueProcessor =
                    scope.ServiceProvider.GetRequiredService<IEmailQueueProcessorService>();

                await queueProcessor.ProcessQueueAsync(_settings.BatchSize, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while processing email queue");
            }

            // Wait for the configured interval before next processing
            await Task.Delay(TimeSpan.FromSeconds(_settings.PollIntervalSeconds), stoppingToken);
        }

        logger.LogInformation("Email Queue Background Service stopped.");
    }
}
