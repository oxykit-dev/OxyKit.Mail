using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using OxyKit.Mail.Contracts;

namespace OxyKit.Mail.Client;

public sealed class MailClientOptions
{
    /// <summary>Base URL, e.g. <c>http://localhost:5026/</c> (include trailing slash preference).</summary>
    public required Uri BaseAddress { get; init; }

    /// <summary>API key value (sent as <see cref="ApiKeyHeader"/> unless <see cref="UseBearerAuth"/>).</summary>
    public string? ApiKey { get; init; }

    /// <summary>Default <c>X-Api-Key</c>.</summary>
    public string ApiKeyHeader { get; init; } = "X-Api-Key";

    /// <summary>When true, send <c>Authorization: Bearer {ApiKey}</c> instead of the API-key header.</summary>
    public bool UseBearerAuth { get; init; }
}

public sealed class MailApiException(int statusCode, string? error, string? responseBody = null)
    : Exception(error ?? $"OxyKit.Mail API returned {statusCode}.")
{
    public int StatusCode { get; } = statusCode;
    public string? Error { get; } = error;
    public string? ResponseBody { get; } = responseBody;
}

/// <summary>
/// Result of <see cref="MailClient.SendAsync"/>. Treat <see cref="SendEmailStatus.Sent"/>,
/// <see cref="SendEmailStatus.Accepted"/>, and <see cref="SendEmailStatus.Scheduled"/> as success — do not re-send.
/// </summary>
public sealed class MailSendResult
{
    public MailSendResult(SendEmailResponse response, int httpStatusCode)
    {
        Response = response ?? throw new ArgumentNullException(nameof(response));
        HttpStatusCode = httpStatusCode;
    }

    public SendEmailResponse Response { get; }
    public int HttpStatusCode { get; }
    public Guid EmailId => Response.EmailId;
    public SendEmailStatus Status => Response.Status;
}

/// <summary>Thin HttpClient SDK for OxyKit.Mail send (not dashboard / identity / DKIM).</summary>
public sealed class MailClient : IDisposable
{
    private static readonly HashSet<SendEmailStatus> AcceptedStatuses =
    [
        SendEmailStatus.Sent,
        SendEmailStatus.Accepted,
        SendEmailStatus.Scheduled,
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    public MailClient(MailClientOptions options)
        : this(CreateHttpClient(options), ownsHttp: true) { }

    public MailClient(HttpClient httpClient)
        : this(httpClient, ownsHttp: false) { }

    private MailClient(HttpClient httpClient, bool ownsHttp)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _ownsHttp = ownsHttp;
    }

    private static HttpClient CreateHttpClient(MailClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var http = new HttpClient { BaseAddress = options.BaseAddress };
        if (!string.IsNullOrWhiteSpace(options.ApiKey))
        {
            if (options.UseBearerAuth)
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                    "Bearer",
                    options.ApiKey
                );
            else
                http.DefaultRequestHeaders.TryAddWithoutValidation(
                    options.ApiKeyHeader,
                    options.ApiKey
                );
        }

        return http;
    }

    /// <summary>
    /// <c>POST /api/v1/email/send</c>. Succeeds on HTTP 200/202 with status sent/accepted/scheduled.
    /// Optional <paramref name="idempotencyKey"/> sets the <c>Idempotency-Key</c> header so duplicate posts return the same acceptance.
    /// </summary>
    public async Task<MailSendResult> SendAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default
    ) => await SendAsync(request, idempotencyKey: null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc cref="SendAsync(SendEmailRequest, CancellationToken)"/>
    public async Task<MailSendResult> SendAsync(
        SendEmailRequest request,
        string? idempotencyKey,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "api/v1/email/send")
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
            httpRequest.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey.Trim());

        using var response = await _http
            .SendAsync(httpRequest, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            await ThrowApiExceptionAsync(response, cancellationToken).ConfigureAwait(false);

        var body = await response
            .Content.ReadFromJsonAsync<SendEmailResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);

        if (body is null || body.EmailId == Guid.Empty || !AcceptedStatuses.Contains(body.Status))
        {
            throw new MailApiException(
                (int)response.StatusCode,
                "Mail accepted the request without a valid emailId/status."
            );
        }

        return new MailSendResult(body, (int)response.StatusCode);
    }

    public void Dispose()
    {
        if (_ownsHttp)
            _http.Dispose();
    }

    private static async Task ThrowApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken
    )
    {
        var raw = await response
            .Content.ReadAsStringAsync(cancellationToken)
            .ConfigureAwait(false);
        string? error = null;
        try
        {
            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            if (doc.RootElement.TryGetProperty("message", out var message))
                error = message.GetString();
            else if (doc.RootElement.TryGetProperty("error", out var err))
                error = err.GetString();
        }
        catch (JsonException)
        {
            // keep raw body
        }

        throw new MailApiException((int)response.StatusCode, error, raw);
    }
}
