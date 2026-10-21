using Microsoft.AspNetCore.Identity;
using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class ListAllRolesEndpoint(RoleManager<IdentityRole> roleManager)
    : MailEndpointWithoutRequest<ListAllRolesResponse>
{
    public override void Configure()
    {
        Get("/roles");
        Version(1);
        Roles(AppRoles.Admin);
        AuthSchemes(AuthSchemeNames.Bearer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var roles = roleManager.Roles.Select(r => r.Name ?? string.Empty).ToList();
        await Send.OkAsync(new ListAllRolesResponse { Roles = roles }, ct);
    }
}
