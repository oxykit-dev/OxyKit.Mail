using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace OxyKit.Mail.Infrastructure.Postgres;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPostgresDataAccess(
        this IServiceCollection services,
        IConfiguration configuration,
        string? migrationsAssembly = null
    )
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PostgreSQLConnection"),
                npgsqlOptions =>
                    npgsqlOptions.MigrationsAssembly(
                        migrationsAssembly ?? "OxyKit.Mail.Infrastructure.Postgres"
                    )
            )
        );
        return services;
    }
}
