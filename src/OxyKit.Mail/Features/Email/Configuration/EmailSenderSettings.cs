using System.ComponentModel.DataAnnotations;

namespace OxyKit.Mail.Features.Email.Configuration;

public record EmailSenderSettings
{
    [Range(1, 100)]
    public int MaxRetries { get; init; } = 3;

    [Range(100, 60000)]
    public int DelayMs { get; init; } = 2000;
}
