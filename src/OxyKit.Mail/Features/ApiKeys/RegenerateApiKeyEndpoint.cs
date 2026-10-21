using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Features.ApiKeys.Services;

namespace OxyKit.Mail.Features.ApiKeys;

public class RegenerateApiKeyEndpoint(IApiKeyService apiKeyService)
    : MailEndpointWithoutRequest<CreateApiKeyResponse>
{
    public override void Configure()
    {
        Post("/apikeys/{id:guid}/regenerate");
        Version(1);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var idStr = Route<string>("id");
        if (!Guid.TryParse(idStr, out var id))
        {
            AddError("Invalid API key id.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(currentUserId))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }
        var oldKeyResult = await apiKeyService.GetApiKeyAsync(currentUserId, id);
        if (await SendOnErrorAsync(oldKeyResult, ct))
            return;

        var oldKey = oldKeyResult.Value;
        await apiKeyService.RevokeApiKeyAsync(id, currentUserId);
        var result = await apiKeyService.CreateApiKeyAsync(currentUserId, oldKey.Label);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.OkAsync(result.Value, ct);
    }
}
