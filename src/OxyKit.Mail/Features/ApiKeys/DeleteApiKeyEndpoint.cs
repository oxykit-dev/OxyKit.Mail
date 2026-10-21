using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Features.ApiKeys.Services;

namespace OxyKit.Mail.Features.ApiKeys;

public class DeleteApiKeyEndpoint(IApiKeyService apiKeyService) : MailEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/apikeys/{id:guid}");
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
        var result = await apiKeyService.RevokeApiKeyAsync(id, currentUserId);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
