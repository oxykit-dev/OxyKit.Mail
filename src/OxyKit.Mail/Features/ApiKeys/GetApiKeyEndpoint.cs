using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Features.ApiKeys.Services;

namespace OxyKit.Mail.Features.ApiKeys;

public class GetApiKeyEndpoint(IApiKeyService apiKeyService)
    : MailEndpointWithoutRequest<ApiKeyResponse>
{
    public override void Configure()
    {
        Get("/apikeys/{id:guid}");
        Version(1);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }
        var id = Route<Guid>("id");
        if (id == Guid.Empty)
        {
            AddError("Invalid API key id.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var result = await apiKeyService.GetApiKeyAsync(currentUserId, id);
        if (await SendOnErrorAsync(result, ct))
            return;

        var key = result.Value;
        await Send.OkAsync(
            new ApiKeyResponse(key.Id, key.Label, key.CreatedAt, key.LastUsedAt),
            cancellation: ct
        );
    }
}
