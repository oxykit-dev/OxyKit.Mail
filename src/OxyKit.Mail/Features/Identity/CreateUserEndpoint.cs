using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class CreateUserEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<CreateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Post("/users");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CreateUserRequest req, CancellationToken ct)
    {
        var result = await userService.CreateUserAsync(req.UserName, req.Email, req.Password);
        if (await SendOnErrorAsync(result, ct))
            return;

        var userResult = await userService.GetUserByUserNameAsync(req.UserName);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var user = userResult.Value;
        var roles = await roleService.GetUserRolesAsync(user);
        await Send.OkAsync(
            new UserResponse(
                user.Id,
                user.UserName ?? string.Empty,
                user.Email ?? string.Empty,
                [.. roles]
            ),
            ct
        );
    }
}
