using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class RemoveRoleEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<RemoveRoleRequest>
{
    public override void Configure()
    {
        Post("/users/roles/remove");
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Version(1);
    }

    public override async Task HandleAsync(RemoveRoleRequest req, CancellationToken ct)
    {
        var userResult = await userService.GetUserByIdAsync(req.UserId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var result = await roleService.RemoveUserFromRoleAsync(userResult.Value, req.Role);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
