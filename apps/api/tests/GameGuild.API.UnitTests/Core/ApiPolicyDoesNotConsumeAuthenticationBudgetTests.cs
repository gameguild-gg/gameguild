using System.Net;
using FluentAssertions;
using GameGuild.API;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Resources;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.UnitTests.Core;

/// <summary>
///     Behavioral regression for issue #1035: the account-settings page fans out
///     GET /v1/auth/session-state and HEAD /v1/auth/external-logins on every render.
///     Those reads moved to the <see cref="RateLimitPolicies.Api"/> policy, so a burst of
///     them (or an exhausted Authentication budget from credential retries) must never
///     block session-state reads, and reads must never consume the Authentication budget.
/// </summary>
public sealed class ApiPolicyDoesNotConsumeAuthenticationBudgetTests
{
    private static Task<IHost> StartHostAsync(RateLimitingOptions options)
    {
        return new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.SetupRateLimiting(new ConfigurationBuilder().Build(), options);
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseRateLimiter();
                    app.UseEndpoints(endpoints => endpoints
                        .MapGet("/v1/auth/session-state", () => Results.Ok())
                        .RequireRateLimiting(RateLimitPolicies.Api)
                        .WithDisplayName("session-state"));
                    app.UseEndpoints(endpoints => endpoints
                        .MapPost("/v1/auth/password-change", () => Results.Ok())
                        .RequireRateLimiting(RateLimitPolicies.Authentication)
                        .WithDisplayName("password-change"));
                }))
            .StartAsync();
    }

    [Fact]
    public async Task ApiPolicyReads_StillPass_WhenTheAuthenticationBudgetIsExhausted()
    {
        var options = RateLimitingOptions.CreateDefault();
        options.AuthenticationRequestsPerMinute = 2;
        options.AuthenticationWindow = TimeSpan.FromMinutes(1);

        using var host = await StartHostAsync(options);
        using var client = host.GetTestClient();

        using var firstMutation = await client.PostAsync("/v1/auth/password-change", content: null);
        using var secondMutation = await client.PostAsync("/v1/auth/password-change", content: null);
        using var blockedMutation = await client.PostAsync("/v1/auth/password-change", content: null);

        firstMutation.StatusCode.Should().Be(HttpStatusCode.OK);
        secondMutation.StatusCode.Should().Be(HttpStatusCode.OK);
        blockedMutation.StatusCode.Should().Be(HttpStatusCode.TooManyRequests,
            "the Authentication budget (2/min here) is exhausted by credential mutations");

        using var read = await client.GetAsync("/v1/auth/session-state");
        read.StatusCode.Should().Be(HttpStatusCode.OK,
            "session-state reads ride the Api policy and must not be blocked by, nor consume, the Authentication budget (issue #1035)");
    }

    [Fact]
    public async Task AuthenticationMutations_StillPass_WhenApiPolicyReadsBurst()
    {
        var options = RateLimitingOptions.CreateDefault();
        options.ApiRequestsPerMinute = 2;
        options.ApiWindow = TimeSpan.FromMinutes(1);

        using var host = await StartHostAsync(options);
        using var client = host.GetTestClient();

        for (var i = 0; i < 4; i++)
        {
            using var read = await client.GetAsync("/v1/auth/session-state");
            read.StatusCode.Should().Be(i < 2 ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests,
                "the Api budget (2/min here) throttles read bursts independently");
        }

        using var mutation = await client.PostAsync("/v1/auth/password-change", content: null);
        mutation.StatusCode.Should().Be(HttpStatusCode.OK,
            "credential mutations keep their own Authentication budget regardless of read bursts (issue #1035)");
    }
}
