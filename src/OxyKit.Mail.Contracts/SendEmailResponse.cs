using System.Text.Json.Serialization;

namespace OxyKit.Mail.Contracts;

/// <summary>
/// Outcome of accepting an email for delivery. Mail owns SMTP; callers treat
/// <see cref="SendEmailStatus.Accepted"/> (and Scheduled) as success — do not re-send.
/// </summary>
public sealed class SendEmailResponse
{
    public Guid EmailId { get; set; }
    public SendEmailStatus Status { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>Delivery-acceptance statuses returned by <c>POST /email/send</c>.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SendEmailStatus
{
    /// <summary>SMTP succeeded immediately (HTTP 200).</summary>
    [JsonStringEnumMemberName("sent")]
    Sent = 0,

    /// <summary>Persisted; Mail will retry / deliver (HTTP 202). Do not re-send.</summary>
    [JsonStringEnumMemberName("accepted")]
    Accepted = 1,

    /// <summary>Persisted for delivery at <c>scheduledFor</c> (HTTP 202).</summary>
    [JsonStringEnumMemberName("scheduled")]
    Scheduled = 2,
}
