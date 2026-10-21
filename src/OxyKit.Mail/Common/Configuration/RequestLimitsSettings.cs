namespace OxyKit.Mail.Common.Configuration;

/// <summary>
/// Configuration settings for HTTP request size limits.
/// Prevents DoS attacks via large payloads.
/// </summary>
public class RequestLimitsSettings
{
    /// <summary>
    /// Maximum allowed HTTP request body size in bytes.
    /// Default: 10 MB (10,485,760 bytes).
    /// </summary>
    public long MaxRequestBodySize { get; set; } = 10 * 1024 * 1024; // 10 MB

    /// <summary>
    /// Maximum allowed email body size in bytes.
    /// Default: 5 MB (5,242,880 bytes).
    /// </summary>
    public long MaxEmailBodySize { get; set; } = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Maximum allowed attachment size per file in bytes.
    /// Default: 5 MB (5,242,880 bytes).
    /// </summary>
    public long MaxAttachmentSize { get; set; } = 5 * 1024 * 1024; // 5 MB
}
