using Microsoft.AspNetCore.Identity;
using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Features.Identity.Services;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Identity;

/// <summary>
/// Endpoint for user authentication.
/// Returns JWT token and refresh token upon successful login.
/// Implements rate limiting to prevent brute force attacks.
/// </summary>
public class LoginEndpoint(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    JwtService jwtService,
    ApplicationDbContext dbContext
) : MailEndpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Version(1);

        // Apply authentication rate limiting
        Options(x => x.RequireRateLimiting(RateLimitingExtensions.AuthPolicy));

        Summary(s =>
        {
            s.Summary = "Authenticate user";
            s.Description =
                "Authenticates user with email/username and password, returns JWT and refresh tokens. "
                + "Rate limited to prevent brute force attacks (10 requests per minute).";
            s.ExampleRequest = new LoginRequest(
                "user@example.com",
                "SecurePassword123!",
                "device-123"
            );
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        // Find user by email or username
        var user =
            await userManager.FindByEmailAsync(req.Identifier)
            ?? await userManager.FindByNameAsync(req.Identifier);

        bool isValid = false;

        if (user != null)
        {
            // Check password
            var result = await signInManager.CheckPasswordSignInAsync(user, req.Password, false);
            isValid = result.Succeeded;
        }
        else
        {
            // Perform dummy operation to maintain timing consistency
            // This prevents timing attacks that could enumerate valid usernames
            await Task.Delay(Random.Shared.Next(50, 150), ct);
        }

        // Generic unauthorized response - don't reveal if user exists
        if (!isValid || user == null)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        // Generate tokens
        var roles = await userManager.GetRolesAsync(user);
        var tokenString = jwtService.GenerateToken(user, roles);

        var refreshToken = new Infrastructure.Entities.RefreshToken
        {
            Token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"),
            DeviceId = req.DeviceId,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow,
            Revoked = false,
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(ct);

        await Send.OkAsync(
            new LoginResponse
            {
                Token = tokenString,
                RefreshToken = refreshToken.Token,
                UserName = user.UserName ?? string.Empty,
            },
            ct
        );
    }
}
