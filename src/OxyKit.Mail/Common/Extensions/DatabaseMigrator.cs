using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Infrastructure;

namespace OxyKit.Mail.Common.Extensions;

public static class DatabaseMigrator
{
    public static void ApplyOxyKitMailMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while applying migrations: {ex.Message}");
        }
    }
}
