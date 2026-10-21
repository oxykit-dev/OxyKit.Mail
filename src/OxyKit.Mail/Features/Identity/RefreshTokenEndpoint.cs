using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Endpoint for refreshing JWT tokens using a refresh token.
/// Implements rate limiting to prevent token abuse.
/// </summary>
public class RefreshTokenEndpoint(
    ApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    JwtService jwtService
) : MailEndpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Version(1);

        // Apply authentication rate limiting
        Options(x => x.RequireRateLimiting(RateLimitingExtensions.AuthPolicy));

        Summary(s =>
        {
            s.Summary = "Refresh JWT token";
            s.Description =
                "Refreshes an expired JWT token using a valid refresh token. "
                + "Rate limited to prevent token abuse (10 requests per minute).";
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = await dbContext
            .RefreshTokens.Include(rt => rt.User)
            .FirstOrDefaultAsync(
                rt => rt.Token == req.RefreshToken && rt.DeviceId == req.DeviceId,
                ct
            );
        if (
            refreshToken == null
            || refreshToken.ExpiresAt < DateTime.UtcNow
            || refreshToken.Revoked
        )
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var user = refreshToken.User;
        if (user == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        var roles = await userManager.GetRolesAsync(user);
        var newJwt = jwtService.GenerateToken(user, roles);
        refreshToken.Revoked = true;
        var newRefreshToken = new Infrastructure.Entities.RefreshToken
        {
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            DeviceId = req.DeviceId,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            Revoked = false,
        };
        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(ct);
        await Send.OkAsync(
            new RefreshTokenResponse { Token = newJwt, RefreshToken = newRefreshToken.Token },
            cancellation: ct
        );
    }
}
