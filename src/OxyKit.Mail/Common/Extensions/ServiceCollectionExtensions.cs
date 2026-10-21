using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OxyKit.Mail.Common.Validators;
using OxyKit.Mail.Features.ApiKeys;
using OxyKit.Mail.Features.Dashboard;
using OxyKit.Mail.Features.Email;
using OxyKit.Mail.Features.Identity;
using OxyKit.Mail.Infrastructure;
using OxyKit.Mail.Infrastructure.Entities;
using OxyKit.Mail.Infrastructure.Postgres;
using OxyKit.Mail.Infrastructure.Repositories;
using OxyKit.Mail.Infrastructure.Sqlite;
using OxyKit.Mail.Infrastructure.SqlServer;

namespace OxyKit.Mail.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds OxyKit.Mail database configuration based on provider.
    /// Supports SqlServer, PostgreSQL, SQLite, and InMemory (for testing).
    /// </summary>
    public static IServiceCollection AddOxyKitMailDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var dbProvider = configuration["DatabaseProvider"];

        // Handle in-memory database for testing
        if (dbProvider == "InMemory")
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OxyKitMailTest")
            );
            return services; // Repositories will be registered by AddOxyKitMailServices -> AddRepositories()
        }

        var migrationsAssembly = dbProvider switch
        {
            "SqlServer" => "OxyKit.Mail.Infrastructure.SqlServer",
            "PostgreSQL" => "OxyKit.Mail.Infrastructure.Postgres",
            "SQLite" => "OxyKit.Mail.Infrastructure.Sqlite",
            _ => null,
        };

        if (dbProvider == "SqlServer")
            services.AddSqlServerDataAccess(configuration, migrationsAssembly);
        else if (dbProvider == "PostgreSQL")
            services.AddPostgresDataAccess(configuration, migrationsAssembly);
        else if (dbProvider == "SQLite")
            services.AddSqliteDataAccess(configuration, migrationsAssembly);

        return services;
    }

    /// <summary>
    /// Adds OxyKit.Mail application and feature services.
    /// </summary>
    public static IServiceCollection AddOxyKitMailServices(
        this IServiceCollection services,
        IConfiguration configuration,
        bool skipBackgroundServices = false
    )
    {
        services.AddRepositories();
        // Feature-specific service registrations (Pure VSA)
        services.AddDashboardFeature();
        services.AddEmailFeature(configuration, skipBackgroundServices);
        services.AddIdentityFeature(configuration);
        services.AddApiKeysFeature();

        return services;
    }

    /// <summary>
    /// Adds FastEndpoints and authentication (JWT + API Key).
    /// </summary>
    public static IServiceCollection AddOxyKitMailAuthentication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddFastEndpoints()
            .AddAuthentication(options =>
            {
                options.DefaultScheme = "OxyKitMailPolicy";
                options.DefaultChallengeScheme = "OxyKitMailPolicy";
            })
            .AddPolicyScheme(
                "OxyKitMailPolicy",
                "JWT or API Key",
                options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var hasApiKey = context.Request.Headers.ContainsKey(
                            ApiKeyAuthHandler.HeaderName
                        );
                        return hasApiKey ? ApiKeyAuthHandler.SchemeName : AuthSchemeNames.Bearer;
                    };
                }
            )
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthHandler>(
                ApiKeyAuthHandler.SchemeName,
                null
            )
            .AddJwtBearer(
                AuthSchemeNames.Bearer,
                options =>
                {
                    var jwtKey = configuration["Jwt:Key"];
                    if (string.IsNullOrEmpty(jwtKey))
                    {
                        throw new InvalidOperationException(
                            "JWT signing key (Jwt:Key) is not configured."
                        );
                    }
                    options.TokenValidationParameters =
                        new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = configuration["Jwt:Issuer"],
                            ValidAudience = configuration["Jwt:Audience"],
                            IssuerSigningKey =
                                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                                    Encoding.UTF8.GetBytes(jwtKey)
                                ),
                        };
                }
            );

        services.AddAuthorization();
        return services;
    }

    /// <summary>
    /// Adds ASP.NET Core Identity with enhanced password policy and custom validators.
    /// </summary>
    /// <remarks>
    /// Password Policy:
    /// - Minimum 12 characters (increased from 8)
    /// - Requires: uppercase, lowercase, digit, special character
    /// - Minimum 4 unique characters (increased from 3)
    /// - Blocks common passwords (password, 123456, etc.)
    /// - Cannot contain username or email
    /// Account Lockout:
    /// - 5 failed attempts (down from 15 minutes)
    /// - 30-minute lockout (increased from 15 minutes)
    /// </remarks>
    public static IServiceCollection AddOxyKitMailIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings (enhanced security)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12; // Increased from 8
                options.Password.RequiredUniqueChars = 4; // Increased from 3

                // Lockout settings (prevent brute force attacks)
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30); // Increased from 15
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // Enable when email is configured
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Add custom password validator
        services.AddScoped<IPasswordValidator<ApplicationUser>, CommonPasswordValidator>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }

    /// <summary>
    /// Adds response compression with HTTPS support.
    /// </summary>
    public static IServiceCollection AddOxyKitMailCompression(this IServiceCollection services)
    {
        services.AddResponseCompression(options => options.EnableForHttps = true);
        return services;
    }

    // ========== Convenience Methods for WebApplicationBuilder ==========

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// <c>Testing</c> always uses InMemory (config override order is unreliable with WebApplicationFactory).
    /// </summary>
    public static void AddOxyKitMailDatabase(this WebApplicationBuilder builder)
    {
        if (
            string.Equals(
                builder.Environment.EnvironmentName,
                "Testing",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("OxyKitMailTest")
            );
            return;
        }

        builder.Services.AddOxyKitMailDatabase(builder.Configuration);
    }

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// Skips background services when <c>EnvironmentName</c> is <c>Testing</c>.
    /// </summary>
    public static void AddOxyKitMailServices(this WebApplicationBuilder builder)
    {
        var skipBackground = string.Equals(
            builder.Environment.EnvironmentName,
            "Testing",
            StringComparison.OrdinalIgnoreCase
        );
        builder.Services.AddOxyKitMailServices(
            builder.Configuration,
            skipBackgroundServices: skipBackground
        );
    }

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// Calls AddOxyKitMailAuthentication(services, configuration).
    /// </summary>
    public static void AddOxyKitMailAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddOxyKitMailAuthentication(builder.Configuration);
    }

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// Calls AddOxyKitMailIdentity(services).
    /// </summary>
    public static void AddOxyKitMailIdentity(this WebApplicationBuilder builder)
    {
        builder.Services.AddOxyKitMailIdentity();
    }

    /// <summary>
    /// Convenience method for WebApplicationBuilder.
    /// Calls AddOxyKitMailCompression(services).
    /// </summary>
    public static void AddOxyKitMailCompression(this WebApplicationBuilder builder)
    {
        builder.Services.AddOxyKitMailCompression();
    }
}
