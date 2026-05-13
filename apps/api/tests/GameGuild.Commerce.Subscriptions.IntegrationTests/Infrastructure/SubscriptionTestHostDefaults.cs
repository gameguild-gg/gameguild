using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace GameGuild.Commerce.Subscriptions.IntegrationTests.Infrastructure;

internal static class SubscriptionTestHostDefaults
{
    public static void ConfigureEnvironment()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("PresentationLayer__Authentication__JwtSecretKey", "subscriptions-integration-tests-jwt-secret-key-1234567890");
        Environment.SetEnvironmentVariable("PresentationLayer__Authentication__JwtIssuer", "GameGuild.Subscriptions.IntegrationTests");
        Environment.SetEnvironmentVariable("PresentationLayer__Authentication__JwtAudience", "GameGuild.Subscriptions.IntegrationTests.Users");
    }

    public static void Configure(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PresentationLayer:Authentication:JwtSecretKey"] = "subscriptions-integration-tests-jwt-secret-key-1234567890",
                ["PresentationLayer:Authentication:JwtIssuer"] = "GameGuild.Subscriptions.IntegrationTests",
                ["PresentationLayer:Authentication:JwtAudience"] = "GameGuild.Subscriptions.IntegrationTests.Users"
            });
        });
    }
}
