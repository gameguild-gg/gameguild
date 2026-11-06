using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class MfaRecoveryRateLimitPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    [Fact]
    public async Task TrustedContactConfirmationUsesAuthenticationBudgetRatherThanGlobalBudget()
    {
        using var factory = fixture.CreateFactory(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PresentationLayer:EnableRateLimiting"] = "true",
                    ["Redis:Enabled"] = "false"
                })));
        var options = factory.Services.GetRequiredService<RateLimitingOptions>();
        // Registered policies capture this shared options instance during startup.
        // Arrange its budget before any HTTP request creates a limiter partition.
        options.AuthenticationRequestsPerMinute = 2;
        options.AuthenticationWindow = TimeSpan.FromMinutes(10);
        options.Limit = 1000;
        options.QueueLimit = 0;
        using var client = factory.CreateClient();
        var body = new { inviteToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)) };
        using var first = await client.PostAsJsonAsync("/v1/auth/trusted-contacts/confirm", body);
        using var second = await client.PostAsJsonAsync("/v1/auth/trusted-contacts/confirm", body);
        using var third = await client.PostAsJsonAsync("/v1/auth/trusted-contacts/confirm", body);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
        Assert.NotNull(third.Headers.RetryAfter);
        Assert.Equal("2", Assert.Single(third.Headers.GetValues("RateLimit-Limit")));

        // This anonymous route has no authentication policy. It must still reach its
        // real unknown-account handler while only the authentication budget is exhausted.
        using var globalControl = await client.PostAsJsonAsync("/v1/auth/mfa/recovery:request",
            new { email = "unknown-recovery-" + Guid.NewGuid().ToString("N") + "@example.test" });
        Assert.Equal(HttpStatusCode.OK, globalControl.StatusCode);
    }
}
