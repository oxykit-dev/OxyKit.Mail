namespace OxyKit.Mail.Common.Extensions;

/// <summary>
/// Extension methods for configuring CORS (Cross-Origin Resource Sharing).
/// </summary>
public static class CorsExtensions
{
    private const string DefaultPolicyName = "AllowFrontend";

    /// <summary>
    /// Adds environment-based CORS configuration to the services.
    /// Reads allowed origins from appsettings configuration or environment variables.
    /// </summary>
    public static IServiceCollection AddOxyKitMailCors(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var allowedOrigins = ResolveOrigins(configuration);
        var allowCredentials = configuration.GetValue<bool?>("Cors:AllowCredentials") ?? false;

        services.AddCors(options =>
        {
            options.AddPolicy(
                DefaultPolicyName,
                policy =>
                {
                    if (allowedOrigins.Length == 0)
                    {
                        policy.DisallowCredentials();
                        policy.WithOrigins();
                    }
                    else
                    {
                        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();

                        if (allowCredentials)
                            policy.AllowCredentials();
                        else
                            policy.DisallowCredentials();
                    }
                }
            );
        });

        return services;
    }

    public static void AddOxyKitMailCors(this WebApplicationBuilder builder)
    {
        builder.Services.AddOxyKitMailCors(builder.Configuration);
    }

    /// <summary>
    /// Adds CORS middleware. Logs a warning when no origins are configured.
    /// </summary>
    public static void UseOxyKitMailCors(this WebApplication app)
    {
        if (ResolveOrigins(app.Configuration).Length == 0)
        {
            app.Logger.LogWarning(
                "No CORS origins configured. Cross-origin requests will be blocked. "
                    + "Configure Cors:AllowedOrigins in appsettings.{{Environment}}.json or set ALLOWED_ORIGINS."
            );
        }

        app.UseCors(DefaultPolicyName);
    }

    private static string[] ResolveOrigins(IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        if (allowedOrigins is { Length: > 0 })
            return allowedOrigins;

        var envOrigins = Environment.GetEnvironmentVariable("ALLOWED_ORIGINS");
        if (string.IsNullOrWhiteSpace(envOrigins))
            return [];

        return
        [
            .. envOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()),
        ];
    }
}
