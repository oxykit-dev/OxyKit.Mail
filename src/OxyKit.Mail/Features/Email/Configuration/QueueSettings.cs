using System.ComponentModel.DataAnnotations;

namespace OxyKit.Mail.Features.Email.Configuration;

public record QueueSettings
{
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; init; } = 10;

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 10;
}
