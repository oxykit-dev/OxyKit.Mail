using System.Reflection;
using Microsoft.AspNetCore.Identity;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Seed;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roleFields = typeof(AppRoles)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(f => f.IsLiteral && !f.IsInitOnly);

        foreach (var field in roleFields)
        {
            var roleName = (string)field.GetRawConstantValue()!;
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
