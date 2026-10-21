using Microsoft.AspNetCore.Identity;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Common.Validators;

/// <summary>
/// Custom password validator that prevents use of common passwords and passwords containing the username.
/// Enhances ASP.NET Core Identity's default password validation.
/// </summary>
public class CommonPasswordValidator : IPasswordValidator<ApplicationUser>
{
    private static readonly string[] CommonPasswords =
    [
        "password",
        "123456",
        "12345678",
        "qwerty",
        "abc123",
        "password123",
        "admin",
        "letmein",
        "welcome",
        "monkey",
        "dragon",
        "master",
        "sunshine",
        "princess",
        "football",
        "iloveyou",
        "trustno1",
        "starwars",
        "passw0rd",
        "Password1",
    ];

    /// <summary>
    /// Validates that the password does not contain common passwords or the username.
    /// </summary>
    /// <param name="manager">The user manager.</param>
    /// <param name="user">The user whose password is being validated.</param>
    /// <param name="password">The password to validate.</param>
    /// <returns>An IdentityResult indicating success or failure with error descriptions.</returns>
    public Task<IdentityResult> ValidateAsync(
        UserManager<ApplicationUser> manager,
        ApplicationUser user,
        string? password
    )
    {
        password ??= string.Empty;

        // Check for common passwords
        if (
            CommonPasswords.Any(common =>
                password.Contains(common, StringComparison.OrdinalIgnoreCase)
            )
        )
        {
            return Task.FromResult(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "CommonPassword",
                        Description = "Password is too common. Choose a more unique password.",
                    }
                )
            );
        }

        // Check if password contains username
        if (
            !string.IsNullOrWhiteSpace(user.UserName)
            && password.Contains(user.UserName, StringComparison.OrdinalIgnoreCase)
        )
        {
            return Task.FromResult(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "PasswordContainsUsername",
                        Description = "Password cannot contain your username.",
                    }
                )
            );
        }

        // Check if password contains email
        if (
            !string.IsNullOrWhiteSpace(user.Email)
            && password.Contains(user.Email.Split('@')[0], StringComparison.OrdinalIgnoreCase)
        )
        {
            return Task.FromResult(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = "PasswordContainsEmail",
                        Description = "Password cannot contain your email address.",
                    }
                )
            );
        }

        return Task.FromResult(IdentityResult.Success);
    }
}
