using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Endpoint for creating the initial admin user.
/// Implements rate limiting to prevent abuse during setup.
/// </summary>
public class CreateAdminEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<CreateAdminRequest, CreateAdminResponse>
{
    public override void Configure()
    {
        Post("/setup/create-admin");
        Version(1);
        AllowAnonymous();
        Options(x => x.RequireRateLimiting(RateLimitingExtensions.AuthPolicy));

        Summary(s =>
        {
            s.Summary = "Creates the initial admin user if none exists.";
            s.Description =
                "Fails if any admin user already exists. Rate limited to prevent abuse (10 requests per minute).";
        });
    }

    public override async Task HandleAsync(CreateAdminRequest req, CancellationToken ct)
    {
        if (await userService.AdminUserExistsAsync())
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var result = await userService.CreateUserAsync(req.UserName, req.Email, req.Password);
        if (await SendOnErrorAsync(result, ct))
            return;

        var userResult = await userService.GetUserByUserNameAsync(req.UserName);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var user = userResult.Value;

        var roleResult = await roleService.AddUserToRoleAsync(user, AppRoles.Admin);
        if (await SendOnErrorAsync(roleResult, ct))
            return;

        await Send.OkAsync(new CreateAdminResponse(true, null), ct);
    }
}
