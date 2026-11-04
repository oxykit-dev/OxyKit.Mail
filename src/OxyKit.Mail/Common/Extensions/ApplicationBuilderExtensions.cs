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
        // Development: Scalar UI + JSON. Testing: JSON only (snapshot export / contract asserts).
        if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
        {
            app.UseOpenApi(c => c.Path = "/openapi/{documentName}.json");
            if (app.Environment.IsDevelopment())
                app.MapScalarApiReference();
        }
        app.MapHealthChecks("/health");
    }
}
