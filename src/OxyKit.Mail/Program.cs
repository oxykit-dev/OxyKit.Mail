using FastEndpoints.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NSwag;
using OxyKit.Mail.Common;
using OxyKit.Mail.Common.Configuration;
using OxyKit.Mail.Common.Extensions;
using OxyKit.Mail.Common.Middleware;
using OxyKit.Mail.Common.Observability;
using OxyKit.Mail.Features.ApiKeys.Configuration;
using OxyKit.Mail.Features.Dashboard;
using OxyKit.Mail.Seed;
using OxyKit.Observability;
using OxyKit.OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

// Load Development .env before observability so OTEL_* / OpenTelemetry__* apply.
builder.ConfigureOxyKitMailEnvironment();
builder.AddOxyKitJsonConsole();
builder.AddOxyKitOpenTelemetry(o =>
{
    o.ServiceName = "OxyKit.Mail";
    o.ActivitySources = [MailTelemetry.ActivitySourceName];
    o.Meters = [MailTelemetry.MeterName];
});
builder.AddOxyKitMailDatabase();
builder.AddOxyKitMailServices();
builder.AddOxyKitMailAuthentication();
builder.AddOxyKitMailIdentity();
builder.AddOxyKitMailCompression();
builder.AddOxyKitMailRateLimiting();

// Add environment-based CORS configuration
builder.AddOxyKitMailCors();

// Configure request limits
builder.Services.Configure<RequestLimitsSettings>(
    builder.Configuration.GetSection("RequestLimits")
);

// Configure API key settings
builder.Services.Configure<ApiKeySettings>(builder.Configuration.GetSection("ApiKeySettings"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.SwaggerDocument(o =>
{
    o.EnableJWTBearerAuth = false;
    o.ShortSchemaNames = true;
    o.DocumentSettings = s =>
    {
        s.DocumentName = "v1";
        s.Title = "OxyKit.Mail API";
        s.Version = "v1";
        s.AddAuth(
            AuthSchemeNames.ApiKey,
            new()
            {
                Name = "X-Api-Key",
                In = OpenApiSecurityApiKeyLocation.Header,
                Type = OpenApiSecuritySchemeType.ApiKey,
            }
        );
        s.AddAuth(
            AuthSchemeNames.Bearer,
            new()
            {
                Type = OpenApiSecuritySchemeType.Http,
                Scheme = JwtBearerDefaults.AuthenticationScheme,
                BearerFormat = "JWT",
            }
        );
    };
    o.ReleaseVersion = 1;
});

builder.Services.AddSignalR();

var app = builder.Build();

// Skip migrations in testing environment (in-memory database doesn't support migrations)
if (app.Environment.EnvironmentName != "Testing")
{
    app.ApplyOxyKitMailMigrations();

    using (var scope = app.Services.CreateScope())
    {
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
    }
}

// Security middleware pipeline (order matters!)
// 0. Correlation (early so all request logs include the id)
app.UseOxyKitCorrelation();

// 1. Security headers
app.UseMiddleware<SecurityHeadersMiddleware>();

// 2. Request size limiting (before reading body)
app.UseMiddleware<RequestSizeLimitMiddleware>();

// 3. Input sanitization (after size check, before processing)
app.UseMiddleware<InputSanitizationMiddleware>();

// 4. Audit logging (logs all requests)
app.UseMiddleware<AuditLoggingMiddleware>();

// Use environment-based CORS
app.UseOxyKitMailCors();
app.UseOxyKitMailRateLimiting();
app.UseOxyKitMailMiddleware();
app.UseOxyKitMailEndpoints();

app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();

// Make the implicit Program class accessible for testing
public partial class Program { }
