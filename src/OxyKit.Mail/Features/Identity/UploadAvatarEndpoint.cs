using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class UploadAvatarEndpoint(IUserService userService) : MailEndpointWithoutRequest<string?>
{
    public override void Configure()
    {
        Post("/users/{id}/avatar");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
        AllowFileUploads();
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
        var file = Files.FirstOrDefault();
        if (file == null || file.Length == 0)
        {
            AddError("No avatar file uploaded.");
            await Send.ErrorsAsync(400, ct);
            return;
        }
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        user.Avatar = ms.ToArray();
        var updateResult = await userService.UpdateUserAsync(user);
        if (await SendOnErrorAsync(updateResult, ct))
            return;

        string? avatarBase64 = user.Avatar != null ? Convert.ToBase64String(user.Avatar) : null;
        await Send.OkAsync(avatarBase64, ct);
    }
}
