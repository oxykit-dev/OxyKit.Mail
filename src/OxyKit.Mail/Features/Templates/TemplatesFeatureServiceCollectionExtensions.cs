using Microsoft.Extensions.Options;

namespace OxyKit.Mail.Features.Templates;

public static class TemplatesFeatureServiceCollectionExtensions
{
    public static IServiceCollection AddTemplatesFeature(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<MailTemplateOptions>()
            .Bind(configuration.GetSection(MailTemplateOptions.SectionName))
            .Validate(
                o =>
                    !string.IsNullOrWhiteSpace(o.TemplatesRoot)
                    && o.MaxSubjectChars > 0
                    && o.MaxBodyChars > 0
                    && o.MaxTemplatePackBytes > 0,
                "MailTemplates limits must be positive and TemplatesRoot must be set."
            )
            .ValidateOnStart();

        services.AddSingleton<IMailTemplateStore, MailTemplateStore>();
        return services;
    }
}
