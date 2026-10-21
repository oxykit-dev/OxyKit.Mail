using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;

namespace OxyKit.Mail.Features.Identity;

public class GetAvatarEndpoint(IUserService userService) : MailEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/users/{id}/avatar");
        Version(1);
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = Route<string>("id");

        if (string.IsNullOrEmpty(userId))
        {
            userId = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        }

        if (string.IsNullOrEmpty(userId))
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var userResult = await userService.GetUserByIdAsync(userId);
        if (await SendOnErrorAsync(userResult, ct))
            return;

        if (userResult.Value.Avatar == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.BytesAsync(
            userResult.Value.Avatar,
            fileName: "avatar.png",
            contentType: "image/png",
            cancellation: ct
        );
    }
}
