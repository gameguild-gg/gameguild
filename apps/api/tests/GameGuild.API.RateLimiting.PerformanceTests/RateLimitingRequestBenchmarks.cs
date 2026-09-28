using BenchmarkDotNet.Attributes;
using GameGuild.API;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace GameGuild.API.PerformanceTests;

/// <summary>
/// Compares a TestServer request with the configured global in-memory rate limiter
/// against the same minimal request pipeline without rate limiting.
/// </summary>
[MemoryDiagnoser]
[MediumRunJob]
public class RateLimitingRequestBenchmarks
{
    private WebApplication _unlimitedApp = null!;
    private WebApplication _limitedApp = null!;
    private HttpClient _unlimitedClient = null!;
    private HttpClient _limitedClient = null!;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _unlimitedApp = await StartServerAsync(enableRateLimiting: false).ConfigureAwait(false);
        _limitedApp = await StartServerAsync(enableRateLimiting: true).ConfigureAwait(false);
        _unlimitedClient = _unlimitedApp.GetTestClient();
        _limitedClient = _limitedApp.GetTestClient();

        using var baseline = await _unlimitedClient.GetAsync("/limited").ConfigureAwait(false);
        using var rateLimited = await _limitedClient.GetAsync("/limited").ConfigureAwait(false);
        if (baseline.StatusCode != System.Net.HttpStatusCode.NoContent ||
            rateLimited.StatusCode != System.Net.HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException("The rate-limiting benchmark endpoints did not return the expected successful response.");
        }
    }

    [Benchmark(Baseline = true, Description = "TestServer request without rate limiting")]
    public async Task<int> UnthrottledRequestAsync()
    {
        using var response = await _unlimitedClient.GetAsync("/limited").ConfigureAwait(false);
        return (int)response.StatusCode;
    }

    [Benchmark(Description = "TestServer request with configured in-memory global rate limit")]
    public async Task<int> InMemoryRateLimitedRequestAsync()
    {
        using var response = await _limitedClient.GetAsync("/limited").ConfigureAwait(false);
        return (int)response.StatusCode;
    }

    [GlobalCleanup]
    public async Task CleanupAsync()
    {
        _unlimitedClient.Dispose();
        _limitedClient.Dispose();
        await _unlimitedApp.DisposeAsync().ConfigureAwait(false);
        await _limitedApp.DisposeAsync().ConfigureAwait(false);
    }

    private static async Task<WebApplication> StartServerAsync(bool enableRateLimiting)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        if (enableRateLimiting)
        {
            builder.Configuration["Redis:Enabled"] = "false";
            var options = RateLimitingOptions.CreateDefault();
            options.Limit = 1_000_000_000;
            options.Period = TimeSpan.FromHours(1);
            options.QueueLimit = 0;
            builder.Services.SetupRateLimiting(builder.Configuration, options);
        }

        var app = builder.Build();
        app.UseRouting();
        if (enableRateLimiting)
        {
            app.UseRateLimiter();
        }
        app.MapGet("/limited", () => Results.NoContent());

        await app.StartAsync().ConfigureAwait(false);
        return app;
    }
}
