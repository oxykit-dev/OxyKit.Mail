using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class ListRolesEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<ListRolesRequest, ListRolesResponse>
{
    public override void Configure()
    {
        Get("/users/roles/list/{UserId}");
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Version(1);
    }

    public override async Task HandleAsync(ListRolesRequest req, CancellationToken ct)
    {
        var userResult = await userService.GetUserByIdAsync(req.UserId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var roles = await roleService.GetUserRolesAsync(userResult.Value);
        await Send.OkAsync(new ListRolesResponse { Roles = [.. roles] }, ct);
    }
}
