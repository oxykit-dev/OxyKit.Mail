using Microsoft.Extensions.DependencyInjection;

namespace OxyKit.Mail.Infrastructure.Repositories;

/// <summary>
/// Extension methods for registering data layer services.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers all repository implementations with their interfaces.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IMessageLogRepository, MessageLogRepository>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

        return services;
    }
}
