using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.ApiKeys.Configuration;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.ApiKeys;

/// <summary>
/// Authentication handler for API Key-based authentication.
/// Implements expiration and inactivity checks for enhanced security.
/// </summary>
public sealed class ApiKeyAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IOptions<ApiKeySettings> apiKeySettings
) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    internal const string SchemeName = "ApiKey";
    internal const string HeaderName = "X-Api-Key";
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<ApiKeyAuthHandler> _logger = logger.CreateLogger<ApiKeyAuthHandler>();
    private readonly ApiKeySettings _apiKeySettings = apiKeySettings.Value;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (IsPublicEndpoint())
        {
            return AuthenticateResult.NoResult();
        }

        if (!Request.Headers.TryGetValue(HeaderName, out var extractedApiKey))
        {
            _logger.LogWarning(
                "API Key was not provided for request from {RemoteIp}",
                Context.Connection.RemoteIpAddress
            );
            return AuthenticateResult.Fail("API Key was not provided.");
        }

        var parts = extractedApiKey.ToString().Trim().Split('.', 2);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var id))
        {
            _logger.LogWarning(
                "Invalid API Key format from {RemoteIp}",
                Context.Connection.RemoteIpAddress
            );
            return AuthenticateResult.Fail("Invalid API credentials!");
        }

        var matchedKey = await _dbContext
            .ApiKeys.Include(k => k.User)
            .FirstOrDefaultAsync(k => k.Id == id);
        if (matchedKey == null)
        {
            _logger.LogWarning(
                "API Key id not found for request from {RemoteIp}",
                Context.Connection.RemoteIpAddress
            );
            return AuthenticateResult.Fail("Invalid API credentials!");
        }
        var hash = ApiKey.HashApiKey($"{id}.{parts[1]}", matchedKey.Salt);
        if (matchedKey.Hash != hash)
        {
            _logger.LogWarning(
                "API Key hash mismatch for request from {RemoteIp}",
                Context.Connection.RemoteIpAddress
            );
            return AuthenticateResult.Fail("Invalid API credentials!");
        }

        // Check if API key has expired (age-based expiration)
        var keyAge = DateTime.UtcNow - matchedKey.CreatedAt;
        if (keyAge.TotalDays > _apiKeySettings.MaxAgeInDays)
        {
            _logger.LogWarning(
                "Expired API Key used: {KeyId}, Age: {Age} days (max {MaxAge}), IP: {RemoteIp}",
                matchedKey.Id,
                (int)keyAge.TotalDays,
                _apiKeySettings.MaxAgeInDays,
                Context.Connection.RemoteIpAddress
            );
            return AuthenticateResult.Fail(
                $"API key expired. Please generate a new key. (Key age: {(int)keyAge.TotalDays} days)"
            );
        }

        // Check if API key is inactive (inactivity-based expiration)
        if (matchedKey.LastUsedAt.HasValue)
        {
            var inactiveDays = (DateTime.UtcNow - matchedKey.LastUsedAt.Value).TotalDays;
            if (inactiveDays > _apiKeySettings.MaxInactiveTimeInDays)
            {
                _logger.LogWarning(
                    "Inactive API Key used: {KeyId}, Inactive: {Days} days (max {MaxInactive}), IP: {RemoteIp}",
                    matchedKey.Id,
                    (int)inactiveDays,
                    _apiKeySettings.MaxInactiveTimeInDays,
                    Context.Connection.RemoteIpAddress
                );
                return AuthenticateResult.Fail(
                    $"API key inactive. Please generate a new key. (Inactive for {(int)inactiveDays} days)"
                );
            }
        }

        // Warn if API key is nearing expiration
        var daysUntilExpiry = _apiKeySettings.MaxAgeInDays - (int)keyAge.TotalDays;
        if (daysUntilExpiry <= _apiKeySettings.WarnBeforeExpiryDays && daysUntilExpiry > 0)
        {
            Context.Response.Headers["X-Api-Key-Expiry-Warning"] =
                $"API key expires in {daysUntilExpiry} day{(daysUntilExpiry == 1 ? "" : "s")}";
            _logger.LogInformation(
                "API Key {KeyId} expires in {Days} days, User: {UserId}",
                matchedKey.Id,
                daysUntilExpiry,
                matchedKey.UserId
            );
        }

        // Update last used timestamp
        matchedKey.LastUsedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        var user = matchedKey.User;
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    bool IsPublicEndpoint() =>
        Context.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().Any() == true;
}
