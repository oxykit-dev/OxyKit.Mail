using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class ClearAvatarEndpoint(IUserService userService) : MailEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/users/{id}/avatar");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<string>("id");
        if (string.IsNullOrEmpty(userId))
        {
            AddError("User id is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var userResult = await userService.GetUserByIdAsync(userId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var user = userResult.Value;
        if (user.Avatar == null)
        {
            await Send.NoContentAsync(ct);
            return;
        }
        user.Avatar = null;
        var updateResult = await userService.UpdateUserAsync(user);
        if (await SendOnErrorAsync(updateResult, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
