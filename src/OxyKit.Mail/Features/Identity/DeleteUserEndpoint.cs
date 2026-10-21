using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class DeleteUserEndpoint(IUserService userService) : MailEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/users/{id:guid}");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id");
        if (string.IsNullOrEmpty(id))
        {
            AddError("User ID is required.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        var userResult = await userService.GetUserByIdAsync(id);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var result = await userService.DeleteUserAsync(userResult.Value);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
