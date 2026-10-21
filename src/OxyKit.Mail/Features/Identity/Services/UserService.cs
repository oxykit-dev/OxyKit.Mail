using ErrorOr;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Identity.Services;

/// <summary>
/// Interface for user management operations.
/// </summary>
public interface IUserService
{
    Task<ErrorOr<ApplicationUser>> GetUserByIdAsync(string userId);
    Task<ErrorOr<ApplicationUser>> GetUserByUserNameAsync(string userName);
    Task<ErrorOr<ApplicationUser>> GetUserByEmailAsync(string email);
    Task<ErrorOr<ApplicationUser>> GetUserByIdentifierAsync(string identifier);
    Task<ErrorOr<IdentityResult>> CreateUserAsync(string userName, string email, string password);
    Task<ErrorOr<IdentityResult>> UpdateUserAsync(ApplicationUser user);
    Task<ErrorOr<IdentityResult>> DeleteUserAsync(ApplicationUser user);
    Task<ErrorOr<IdentityResult>> SetUserPasswordAsync(ApplicationUser user, string newPassword);
    Task<ErrorOr<bool>> ValidatePasswordAsync(ApplicationUser user, string password);
    Task<bool> AdminUserExistsAsync();
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task<ErrorOr<ApplicationUser>> ValidateUserAsync(string identifier, string password);
}

/// <summary>
/// Service for user management operations.
/// </summary>
public class UserService(UserManager<ApplicationUser> userManager, ILogger<UserService> logger)
    : IUserService
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    public async Task<ErrorOr<ApplicationUser>> GetUserByIdAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("User with ID {UserId} not found.", userId);
            return Error.NotFound(description: $"User with ID {userId} not found.");
        }
        return user;
    }

    /// <summary>
    /// Gets a user by their username.
    /// </summary>
    public async Task<ErrorOr<ApplicationUser>> GetUserByUserNameAsync(string userName)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user == null)
        {
            logger.LogWarning("User with username {UserName} not found.", userName);
            return Error.NotFound(description: $"User with username {userName} not found.");
        }
        return user;
    }

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    public async Task<ErrorOr<ApplicationUser>> GetUserByEmailAsync(string email)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            logger.LogWarning("User with email {Email} not found.", email);
            return Error.NotFound(description: $"User with email {email} not found.");
        }
        return user;
    }

    /// <summary>
    /// Gets a user by their username or email (identifier).
    /// </summary>
    public async Task<ErrorOr<ApplicationUser>> GetUserByIdentifierAsync(string identifier)
    {
        var user =
            await userManager.FindByNameAsync(identifier)
            ?? await userManager.FindByEmailAsync(identifier);
        if (user == null)
        {
            logger.LogWarning("User with identifier {Identifier} not found.", identifier);
            return Error.NotFound(description: $"User with identifier {identifier} not found.");
        }
        return user;
    }

    /// <summary>
    /// Creates a new user with the specified credentials.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> CreateUserAsync(
        string userName,
        string email,
        string password
    )
    {
        var user = new ApplicationUser { UserName = userName, Email = email };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Failed to create user {UserName}: {Errors}",
                userName,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> UpdateUserAsync(ApplicationUser user)
    {
        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Failed to update user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }

    /// <summary>
    /// Deletes a user from the system.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> DeleteUserAsync(ApplicationUser user)
    {
        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Failed to delete user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }

    /// <summary>
    /// Sets a new password for the specified user.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> SetUserPasswordAsync(
        ApplicationUser user,
        string newPassword
    )
    {
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
        {
            logger.LogWarning(
                "Failed to set password for user {UserId}: {Errors}",
                user.Id,
                string.Join(", ", result.Errors.Select(e => e.Description))
            );
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }

    /// <summary>
    /// Validates a user's password.
    /// </summary>
    public async Task<ErrorOr<bool>> ValidatePasswordAsync(ApplicationUser user, string password)
    {
        var valid = await userManager.CheckPasswordAsync(user, password);
        if (!valid)
        {
            logger.LogWarning("Invalid password attempt for user {UserId}.", user.Id);
            return Error.Validation(description: "Invalid password.");
        }
        return valid;
    }

    /// <summary>
    /// Checks if at least one admin user exists in the system.
    /// </summary>
    public async Task<bool> AdminUserExistsAsync()
    {
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var exists = admins.Any();
        if (!exists)
        {
            logger.LogWarning("No admin users found in the system.");
        }
        return exists;
    }

    /// <summary>
    /// Gets all users in the system.
    /// </summary>
    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await userManager.Users.ToListAsync();
    }

    /// <summary>
    /// Validates a user's credentials (username/email and password).
    /// </summary>
    public async Task<ErrorOr<ApplicationUser>> ValidateUserAsync(
        string identifier,
        string password
    )
    {
        var userResult = await GetUserByIdentifierAsync(identifier);
        if (userResult.IsError)
            return userResult.Errors;
        var user = userResult.Value;
        if (
            user.LockoutEnabled
            && user.LockoutEnd.HasValue
            && user.LockoutEnd > DateTimeOffset.UtcNow
        )
        {
            logger.LogWarning("User {UserId} is locked out and cannot log in.", user.Id);
            return Error.Forbidden(description: "User is locked out.");
        }
        var validResult = await ValidatePasswordAsync(user, password);
        if (validResult.IsError)
            return validResult.Errors;
        return user;
    }
}
