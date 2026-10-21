using OxyKit.Mail.Features.Identity.Configuration;
using OxyKit.Mail.Features.Identity.Services;

namespace OxyKit.Mail.Features.Identity;

public static class IdentityFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddIdentityFeature(
        this IServiceCollection services,
        IConfiguration? configuration = null
    )
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();

        if (configuration != null)
        {
            services.Configure<JwtConfig>(configuration.GetSection("Jwt"));
        }
        services.AddSingleton<JwtService>();

        return services;
    }
}
