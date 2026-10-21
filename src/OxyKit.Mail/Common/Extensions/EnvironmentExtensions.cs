using DotNetEnv;

namespace OxyKit.Mail.Common.Extensions;

public static class EnvironmentExtensions
{
    public static void ConfigureOxyKitMailEnvironment(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment())
        {
            Env.Load("./.env");
        }
        builder.Configuration.AddEnvironmentVariables();
    }
}
