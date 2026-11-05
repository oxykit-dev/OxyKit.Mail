using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Lets the signed-in user replace their own password. Bearer only — an API key has no password.
/// A wrong current password is 400, not 401, so clients do not treat it as a signed-out session.
/// </summary>
public class ChangePasswordEndpoint(IUserService userService) : MailEndpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Post("/auth/change-password");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User, AppRoles.Sender);
        AuthSchemes(AuthSchemeNames.Bearer);
        Options(x => x.RequireRateLimiting(RateLimitingExtensions.AuthPolicy));
        Summary(s =>
        {
            s.Summary = "Change the signed-in user's password";
            s.Description =
                "Requires the current password. Identity password policy still applies. "
                + "Rate limited with the other auth routes.";
            s.ExampleRequest = new ChangePasswordRequest(
                "CurrentPassword123!",
                "NewPassword456!",
                "NewPassword456!"
            );
        });
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var userResult = await userService.GetUserByIdAsync(userId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        var result = await userService.ChangePasswordAsync(
            userResult.Value,
            req.CurrentPassword,
            req.NewPassword
        );
        if (await SendOnErrorAsync(result, ct))
            return;

        await Send.NoContentAsync(ct);
    }
}
