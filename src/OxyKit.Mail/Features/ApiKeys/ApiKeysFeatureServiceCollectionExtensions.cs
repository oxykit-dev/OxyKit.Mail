using OxyKit.Mail.Features.ApiKeys.Services;

namespace OxyKit.Mail.Features.ApiKeys;

public static class ApiKeysFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddApiKeysFeature(this IServiceCollection services)
    {
        services.AddScoped<IApiKeyService, ApiKeyService>();
        return services;
    }
}
