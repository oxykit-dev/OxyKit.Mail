using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Common;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Features.Identity;

public class RevokeRefreshTokenEndpoint(ApplicationDbContext dbContext)
    : MailEndpoint<RevokeRefreshTokenRequest>
{
    public override void Configure()
    {
        Post("/auth/revoke");
        AllowAnonymous();
        Version(1);
    }

    public override async Task HandleAsync(RevokeRefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(
            rt => rt.Token == req.RefreshToken && rt.DeviceId == req.DeviceId,
            ct
        );
        if (refreshToken?.Revoked != false)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        refreshToken.Revoked = true;
        await dbContext.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
