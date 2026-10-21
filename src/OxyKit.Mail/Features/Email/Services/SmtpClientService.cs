using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace OxyKit.Mail.Features.Email.Services;

/// <summary>
/// Service interface for SMTP client operations.
/// </summary>
public interface ISmtpClientService : IDisposable
{
    /// <summary>
    /// Connects to the SMTP server.
    /// </summary>
    Task ConnectAsync(
        string host,
        int port,
        SecureSocketOptions options,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Authenticates with the SMTP server using username and password.
    /// </summary>
    Task AuthenticateAsync(string userName, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates with the SMTP server using SASL XOAUTH2.
    /// </summary>
    Task AuthenticateOAuth2Async(
        string userName,
        string accessToken,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Sends a MIME message via SMTP.
    /// </summary>
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects from the SMTP server.
    /// </summary>
    Task DisconnectAsync(bool quit, CancellationToken cancellationToken);
}

/// <summary>
/// Service for SMTP client operations.
/// </summary>
public class SmtpClientService : ISmtpClientService
{
    private readonly SmtpClient _client = new();
    private bool _disposed;

    /// <summary>
    /// Connects to the SMTP server.
    /// </summary>
    public Task ConnectAsync(
        string host,
        int port,
        SecureSocketOptions options,
        CancellationToken cancellationToken
    ) => _client.ConnectAsync(host, port, options, cancellationToken);

    /// <summary>
    /// Authenticates with the SMTP server using username and password.
    /// </summary>
    public Task AuthenticateAsync(
        string userName,
        string password,
        CancellationToken cancellationToken
    ) => _client.AuthenticateAsync(userName, password, cancellationToken);

    /// <summary>
    /// Authenticates with the SMTP server using SASL XOAUTH2.
    /// </summary>
    public Task AuthenticateOAuth2Async(
        string userName,
        string accessToken,
        CancellationToken cancellationToken
    )
    {
        var oauth2 = new SaslMechanismOAuth2(userName, accessToken);
        return _client.AuthenticateAsync(oauth2, cancellationToken);
    }

    /// <summary>
    /// Sends a MIME message via SMTP.
    /// </summary>
    public Task SendAsync(MimeMessage message, CancellationToken cancellationToken) =>
        _client.SendAsync(message, cancellationToken);

    /// <summary>
    /// Disconnects from the SMTP server.
    /// </summary>
    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken) =>
        _client.DisconnectAsync(quit, cancellationToken);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _client.Dispose();
            }
            _disposed = true;
        }
    }
}
