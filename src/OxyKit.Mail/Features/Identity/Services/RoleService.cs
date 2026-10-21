using ErrorOr;
using Microsoft.AspNetCore.Identity;
using OxyKit.Mail.Infrastructure.Entities;

namespace OxyKit.Mail.Features.Identity.Services;

/// <summary>
/// Service contract for role management operations.
/// </summary>
public interface IRoleService
{
    Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
    Task<ErrorOr<IdentityResult>> AddUserToRoleAsync(ApplicationUser user, string role);
    Task<ErrorOr<IdentityResult>> RemoveUserFromRoleAsync(ApplicationUser user, string role);
}

/// <summary>
/// Service for role management operations.
/// </summary>
public class RoleService(UserManager<ApplicationUser> userManager) : IRoleService
{
    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user) =>
        await userManager.GetRolesAsync(user);

    /// <summary>
    /// Adds a user to a role.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> AddUserToRoleAsync(ApplicationUser user, string role)
    {
        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }

    /// <summary>
    /// Removes a user from a role.
    /// </summary>
    public async Task<ErrorOr<IdentityResult>> RemoveUserFromRoleAsync(
        ApplicationUser user,
        string role
    )
    {
        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return Error.Validation(
                description: string.Join("; ", result.Errors.Select(e => e.Description))
            );
        }
        return result;
    }
}
