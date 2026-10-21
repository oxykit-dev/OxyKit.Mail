using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OxyKit.Mail.Infrastructure.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        string? migrationsAssembly = null
    )
    {
        var connectionString = configuration.GetConnectionString("SQLiteConnection");

        if (!string.IsNullOrEmpty(connectionString))
        {
            const string dataSourcePrefix = "Data Source=";
            var startIndex = connectionString.IndexOf(
                dataSourcePrefix,
                StringComparison.OrdinalIgnoreCase
            );
            if (startIndex >= 0)
            {
                var filePath = connectionString[(startIndex + dataSourcePrefix.Length)..].Trim();
                var directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlite(
                connectionString,
                sqliteOptions =>
                    sqliteOptions.MigrationsAssembly(
                        migrationsAssembly ?? "OxyKit.Mail.Infrastructure.Sqlite"
                    )
            )
        );
        return services;
    }
}
