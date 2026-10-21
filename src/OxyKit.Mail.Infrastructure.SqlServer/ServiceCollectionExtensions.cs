using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OxyKit.Mail.Infrastructure.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        string? migrationsAssembly = null
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("SqlServerConnection"),
                sqlOptions =>
                    sqlOptions.MigrationsAssembly(
                        migrationsAssembly ?? "OxyKit.Mail.Infrastructure.SqlServer"
                    )
            )
        );
        return services;
    }
}
