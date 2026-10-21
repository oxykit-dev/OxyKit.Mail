using OxyKit.Mail.Features.Dashboard.Services;

namespace OxyKit.Mail.Features.Dashboard;

public static class DashboardFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddDashboardFeature(this IServiceCollection services)
    {
        // Dashboard services
        services.AddScoped<IDashboardService, DashboardService>();

        return services;
    }
}
