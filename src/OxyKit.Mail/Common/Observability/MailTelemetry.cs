using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace OxyKit.Mail.Common.Observability;

public static class MailTelemetry
{
    public const string ActivitySourceName = "OxyKit.Mail";
    public const string MeterName = "OxyKit.Mail";

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    public static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> MessagesQueued = Meter.CreateCounter<long>(
        "oxykit.mail.messages.queued"
    );
    public static readonly Counter<long> MessagesSent = Meter.CreateCounter<long>(
        "oxykit.mail.messages.sent"
    );
    public static readonly Counter<long> MessagesFailed = Meter.CreateCounter<long>(
        "oxykit.mail.messages.failed"
    );
    public static readonly Histogram<double> SendDurationMs = Meter.CreateHistogram<double>(
        "oxykit.mail.messages.send.duration",
        unit: "ms"
    );
}
