using Microsoft.Extensions.Options;
using OxyKit.Mail.Features.Dashboard;
using OxyKit.Mail.Features.Email.Configuration;
using OxyKit.Mail.Features.Email.Services;

namespace OxyKit.Mail.Features.Email;

public static class EmailFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddEmailFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        bool skipBackgroundServices = false
    )
    {
        // Configuration options
        services.AddSingleton<IValidateOptions<SmtpSettings>, SmtpSettingsValidator>();
        services
            .AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection("SmtpSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<QueueSettings>()
            .Bind(configuration.GetSection("QueueSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services
            .AddOptions<EmailSenderSettings>()
            .Bind(configuration.GetSection("EmailSenderSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<DkimSettings>, DkimSettingsValidator>();
        services
            .AddOptions<DkimSettings>()
            .Bind(configuration.GetSection("DkimSettings"))
            .ValidateOnStart();

        // Email services
        services.AddHttpClient(SmtpAccessTokenProvider.HttpClientName);
        services.AddSingleton<ISmtpAccessTokenProvider, SmtpAccessTokenProvider>();
        services.AddScoped<IEmailService, EmailService<DashboardHub>>();
        services.AddScoped<IEmailQueueProcessorService, EmailQueueProcessorService<DashboardHub>>();
        services.AddScoped<ISenderService, SenderService>();
        services.AddScoped<ISmtpClientService, SmtpClientService>();
        services.AddScoped<IDkimService, DkimService>();
        services.AddScoped<IKeyStorage, FileSystemKeyStorage>();

        // Background service registration
        if (!skipBackgroundServices)
        {
            services.AddHostedService<QueueBackgroundService>();
        }

        // Health checks
        services.AddHealthChecks().AddCheck<SmtpHealthCheck>("smtp");

        return services;
    }
}
