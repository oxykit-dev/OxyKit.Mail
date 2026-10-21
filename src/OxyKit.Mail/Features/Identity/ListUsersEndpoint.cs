using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class ListUsersEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpointWithoutRequest<List<UserResponse>>
{
    public override void Configure()
    {
        Get("/users");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!User.IsInRole(AppRoles.Admin))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }
        var users = await userService.GetAllUsersAsync();

        var response = new List<UserResponse>();
        foreach (var u in users)
        {
            var roles = await roleService.GetUserRolesAsync(u);
            response.Add(
                new UserResponse(
                    u.Id,
                    u.UserName ?? string.Empty,
                    u.Email ?? string.Empty,
                    [.. roles]
                )
            );
        }
        await Send.OkAsync(response, ct);
    }
}
