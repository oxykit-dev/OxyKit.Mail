using OxyKit.Mail.Common;
using OxyKit.Mail.Features.Identity.Services;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Endpoint to check if an admin user exists in the system.
/// </summary>
public class NeedsAdminEndpoint(IUserService userService)
    : MailEndpointWithoutRequest<NeedsAdminResponse>
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/setup/needs-admin");
        Version(1);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Checks if an admin user exists.";
            s.Description = "Returns needsAdmin=true if no admin user exists, otherwise false.";
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        bool needsAdmin = !await userService.AdminUserExistsAsync();
        await Send.OkAsync(new NeedsAdminResponse(needsAdmin), ct);
    }
}
