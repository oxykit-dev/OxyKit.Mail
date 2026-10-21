using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class AssignRoleEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<AssignRoleRequest>
{
    public override void Configure()
    {
        Post("/users/roles/assign");
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Version(1);
    }

    public override async Task HandleAsync(AssignRoleRequest req, CancellationToken ct)
    {
        var userResult = await userService.GetUserByIdAsync(req.UserId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var result = await roleService.AddUserToRoleAsync(userResult.Value, req.Role);
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
