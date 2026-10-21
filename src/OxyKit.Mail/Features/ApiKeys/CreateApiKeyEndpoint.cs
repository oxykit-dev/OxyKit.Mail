using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Features.ApiKeys.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.ApiKeys;

/// <summary>
/// Endpoint for creating API keys.
/// Implements rate limiting to prevent abuse.
/// </summary>
public class CreateApiKeyEndpoint(IApiKeyService apiKeyService)
    : MailEndpoint<CreateApiKeyRequest, CreateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/apikeys");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);

        // Apply authentication rate limiting
        Options(x => x.RequireRateLimiting(RateLimitingExtensions.AuthPolicy));

        Summary(s =>
        {
            s.Summary = "Create a new API key";
            s.Description =
                "Creates a new API key for the current user. "
                + "Rate limited to prevent abuse (10 requests per minute).";
        });
    }

    public override async Task HandleAsync(CreateApiKeyRequest req, CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await apiKeyService.CreateApiKeyAsync(currentUserId, req.Label!);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.OkAsync(result.Value, cancellation: ct);
    }
}
