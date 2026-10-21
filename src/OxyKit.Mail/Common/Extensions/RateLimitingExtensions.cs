using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using OxyKit.Mail.Common.Configuration;

namespace OxyKit.Mail.Common.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Rate limiter policy name for authentication endpoints.
    /// </summary>
    public const string AuthPolicy = "auth";

    /// <summary>
    /// Rate limiter policy name for general API endpoints.
    /// </summary>
    public const string ApiPolicy = "api";

    /// <summary>
    /// Rate limiter policy name for email sending endpoints.
    /// </summary>
    public const string EmailPolicy = "email";

    /// <summary>
    /// Adds rate limiting services.
    /// </summary>
    public static IServiceCollection AddOxyKitMailRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Bind rate limit settings from configuration
        var rateLimitSettings = new RateLimitSettings();
        configuration.GetSection("RateLimit").Bind(rateLimitSettings);

        services.AddRateLimiter(options =>
        {
            // Authentication endpoint rate limiter (stricter limits)
            options.AddFixedWindowLimiter(
                AuthPolicy,
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = rateLimitSettings.Auth.PermitLimit;
                    limiterOptions.Window = TimeSpan.FromSeconds(
                        rateLimitSettings.Auth.WindowSeconds
                    );
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = rateLimitSettings.Auth.QueueLimit;
                }
            );

            // Email endpoint rate limiter (prevent spam/abuse)
            options.AddFixedWindowLimiter(
                EmailPolicy,
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = rateLimitSettings.Email.PermitLimit;
                    limiterOptions.Window = TimeSpan.FromSeconds(
                        rateLimitSettings.Email.WindowSeconds
                    );
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = rateLimitSettings.Email.QueueLimit;
                }
            );

            // General API rate limiter (more permissive)
            options.AddFixedWindowLimiter(
                ApiPolicy,
                limiterOptions =>
                {
                    limiterOptions.PermitLimit = rateLimitSettings.Api.PermitLimit;
                    limiterOptions.Window = TimeSpan.FromSeconds(
                        rateLimitSettings.Api.WindowSeconds
                    );
                    limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                    limiterOptions.QueueLimit = rateLimitSettings.Api.QueueLimit;
                }
            );

            // Global rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "Too many requests",
                            message = "Rate limit exceeded. Please try again later.",
                            retryAfter = retryAfter.TotalSeconds,
                        },
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    await context.HttpContext.Response.WriteAsJsonAsync(
                        new
                        {
                            error = "Too many requests",
                            message = "Rate limit exceeded. Please try again later.",
                        },
                        cancellationToken: cancellationToken
                    );
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// Calls AddOxyKitMailRateLimiting(services, configuration).
    /// </summary>
    public static void AddOxyKitMailRateLimiting(this WebApplicationBuilder builder)
    {
        builder.Services.AddOxyKitMailRateLimiting(builder.Configuration);
    }

    /// <summary>
    /// Adds rate limiting middleware to the application pipeline.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static void UseOxyKitMailRateLimiting(this WebApplication app)
    {
        app.UseRateLimiter();
    }
}
