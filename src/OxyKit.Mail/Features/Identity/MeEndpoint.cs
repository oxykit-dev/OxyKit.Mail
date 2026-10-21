using System.Security.Claims;
using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class MeEndpoint : MailEndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");
        Version(1);
        Roles(AppRoles.Admin, AppRoles.User, AppRoles.Sender);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = new MeResponse(
            User.Identity?.Name,
            [.. User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)]
        );

        await Send.OkAsync(response, ct);
    }
}
