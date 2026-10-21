using FastEndpoints;
using OxyKit.Mail.Common.Middleware;
using Scalar.AspNetCore;

namespace OxyKit.Mail.Common.Extensions;

public static class ApplicationBuilderExtensions
{
    public static void UseOxyKitMailMiddleware(this WebApplication app)
    {
        app.UseExceptionHandling();
        app.UseAuthentication().UseAuthorization();
        if (!app.Environment.IsDevelopment())
            app.UseResponseCompression();
    }

    public static void UseOxyKitMailEndpoints(this WebApplication app)
    {
        app.UseFastEndpoints(c =>
        {
            c.Endpoints.RoutePrefix = "api";
            c.Versioning.Prefix = "v";
            c.Versioning.PrependToRoute = true;
        });
        if (app.Environment.IsDevelopment())
        {
            // Suite convention: /openapi/v1.json + /scalar
            app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
            app.MapScalarApiReference();
        }
        app.MapHealthChecks("/health");
    }
}
