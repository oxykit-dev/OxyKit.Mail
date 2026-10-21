using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Features.ApiKeys.Services;

namespace OxyKit.Mail.Features.ApiKeys;

public class ListApiKeysEndpoint(IApiKeyService apiKeyService)
    : MailEndpointWithoutRequest<List<ApiKeyResponse>>
{
    public override void Configure()
    {
        Get("/apikeys");
        Version(1);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var currentUserId =
            User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("UserId");
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }
        var keys = await apiKeyService.GetApiKeysForUserAsync(currentUserId);

        var response = keys.ConvertAll(k => new ApiKeyResponse(
            k.Id,
            k.Label,
            k.CreatedAt,
            k.LastUsedAt
        ));
        await Send.OkAsync(response, ct);
    }
}
