using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Endpoint for updating user information.
/// Requires Admin role authorization.
/// </summary>
public class UpdateUserEndpoint(IUserService userService, IRoleService roleService)
    : MailEndpoint<UpdateUserRequest, UserResponse>
{
    public override void Configure()
    {
        Put("/users/{id}");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        Summary(s =>
        {
            s.Summary = "Update user information";
            s.Description =
                "Updates user details including username, email, and password. Requires Admin role.";
            s.ExampleRequest = new UpdateUserRequest(
                Id: "user-id-123",
                UserName: "newusername",
                Email: "newemail@example.com",
                Password: null,
                ConfirmPassword: null
            );
        });
    }

    public override async Task HandleAsync(UpdateUserRequest req, CancellationToken ct)
    {
        var userResult = await userService.GetUserByIdAsync(req.Id);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var user = userResult.Value;

        // Validate and update email
        if (!string.IsNullOrWhiteSpace(req.Email) && req.Email != user.Email)
        {
            var emailExists = await userService.GetUserByEmailAsync(req.Email);
            if (!emailExists.IsError && emailExists.Value.Id != req.Id)
            {
                AddError(r => r.Email, "Email is already in use by another user.");
                await Send.ErrorsAsync(400, ct);
                return;
            }
            user.Email = req.Email;
        }

        // Validate and update username
        if (!string.IsNullOrWhiteSpace(req.UserName) && req.UserName != user.UserName)
        {
            var usernameExists = await userService.GetUserByUserNameAsync(req.UserName);
            if (!usernameExists.IsError && usernameExists.Value.Id != req.Id)
            {
                AddError(r => r.UserName, "Username is already in use by another user.");
                await Send.ErrorsAsync(400, ct);
                return;
            }
            user.UserName = req.UserName;
        }

        var updateResult = await userService.UpdateUserAsync(user);
        if (await SendOnErrorAsync(updateResult, ct))
            return;

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            var pwResult = await userService.SetUserPasswordAsync(user, req.Password);
            if (await SendOnErrorAsync(pwResult, ct))
                return;
        }

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
