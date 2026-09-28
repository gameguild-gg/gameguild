using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using GameGuild.API;
using GameGuild.API.Core.Middleware;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using GameGuild.Resources;
using GameGuild.Identity.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Microsoft.AspNetCore.RateLimiting;
using Xunit.Abstractions;

namespace GameGuild.Resources.IntegrationTests.Security;

[CollectionDefinition("Redis rate limiter")]
public sealed class RedisRateLimiterCollection : ICollectionFixture<RedisRateLimiterFixture>
{
}

public sealed class RedisRateLimiterFixture : IAsyncLifetime
{
    private readonly IContainer _container = new ContainerBuilder()
        .WithImage("redis:7-alpine")
        .WithPortBinding(6379, true)
        .WithCleanUp(true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilCommandIsCompleted(["redis-cli", "ping"]))
        .Build();

    private readonly List<IConnectionMultiplexer> _connections = [];

    public IReadOnlyList<IConnectionMultiplexer> Connections => _connections;

    public string RedisEndpoint { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        RedisEndpoint = $"{_container.Hostname}:{_container.GetMappedPublicPort(6379)}";
        for (var index = 0; index < 4; index++)
        {
            _connections.Add(await ConnectionMultiplexer.ConnectAsync(RedisEndpoint));
        }
    }

    public async Task DisposeAsync()
    {
        foreach (var connection in _connections)
        {
            await connection.DisposeAsync();
        }

        await _container.DisposeAsync();
    }
}

[Collection("Redis rate limiter")]
[Trait("Category", "Integration")]
[Trait("Infrastructure", "Redis")]
public sealed class RedisDistributedRateLimiterIntegrationTests(
    RedisRateLimiterFixture fixture,
    ITestOutputHelper output)
{
    private const string ProbeHostAssemblyFileName = "GameGuild.RateLimitingProbeHost.dll";

    [Fact]
    public async Task SeparateApiHostsShareRedisLimitUnderConcurrentLoad()
    {
        const int requestLimit = 20;
        const int requestCount = 200;
        var userId = Guid.NewGuid().ToString("N");
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = requestLimit;
        options.Period = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        options.EnableProgressivePenalties = false;

        using var firstHost = await StartRateLimitedTestHostAsync(fixture.Connections[0], options);
        using var secondHost = await StartRateLimitedTestHostAsync(fixture.Connections[1], options);
        using var firstClient = firstHost.GetTestClient();
        using var secondClient = secondHost.GetTestClient();

        var stopwatch = Stopwatch.StartNew();
        var measurements = await Task.WhenAll(Enumerable.Range(0, requestCount).Select(async index =>
        {
            var client = index % 2 == 0 ? firstClient : secondClient;
            using var request = new HttpRequestMessage(HttpMethod.Get, "/limited");
            request.Headers.Add("X-Test-User", userId);
            var startedAt = Stopwatch.GetTimestamp();
            var response = await client.SendAsync(request);
            return (Response: response, Latency: Stopwatch.GetElapsedTime(startedAt));
        }));
        stopwatch.Stop();
        var responses = measurements.Select(measurement => measurement.Response).ToArray();

        try
        {
            var accepted = responses.Count(response => response.StatusCode == HttpStatusCode.NoContent);
            var rejected = responses.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests);
            accepted.Should().Be(requestLimit);
            rejected.Should().Be(requestCount - requestLimit);
            responses.Should().OnlyContain(response =>
                response.StatusCode == HttpStatusCode.NoContent ||
                response.StatusCode == HttpStatusCode.TooManyRequests);

            output.WriteLine(
                "Two TestServer API hosts enforced one Redis limit across {0} concurrent requests in {1:F1} ms ({2:F0} requests/sec; p95 {3:F1} ms, p99 {4:F1} ms).",
                requestCount,
                stopwatch.Elapsed.TotalMilliseconds,
                requestCount / stopwatch.Elapsed.TotalSeconds,
                Percentile(measurements.Select(measurement => measurement.Latency), 0.95).TotalMilliseconds,
                Percentile(measurements.Select(measurement => measurement.Latency), 0.99).TotalMilliseconds);
        }
        finally
        {
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }
    }

    [Fact]
    public async Task SeparateOperatingSystemProcessesShareRedisLimitUnderConcurrentLoad()
    {
        const int requestLimit = 20;
        const int requestCount = 200;
        var userId = Guid.NewGuid().ToString("N");
        var firstPort = GetFreeTcpPort();
        var secondPort = GetFreeTcpPort();
        while (secondPort == firstPort)
        {
            secondPort = GetFreeTcpPort();
        }

        var firstHost = StartRateLimitingProbeHost(firstPort, requestLimit);
        var secondHost = StartRateLimitingProbeHost(secondPort, requestLimit);
        try
        {
            await Task.WhenAll(WaitForProbeHostAsync(firstHost), WaitForProbeHostAsync(secondHost));
            using var firstClient = new HttpClient { BaseAddress = firstHost.BaseAddress };
            using var secondClient = new HttpClient { BaseAddress = secondHost.BaseAddress };
            var responses = await Task.WhenAll(Enumerable.Range(0, requestCount).Select(async index =>
            {
                var client = index % 2 == 0 ? firstClient : secondClient;
                using var request = new HttpRequestMessage(HttpMethod.Get, "/limited");
                request.Headers.Add("X-Test-User", userId);
                return await client.SendAsync(request);
            }));

            try
            {
                responses.Count(response => response.StatusCode == HttpStatusCode.NoContent).Should().Be(requestLimit);
                responses.Count(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                    .Should().Be(requestCount - requestLimit);
                responses.Should().OnlyContain(response =>
                    response.StatusCode == HttpStatusCode.NoContent ||
                    response.StatusCode == HttpStatusCode.TooManyRequests);
                output.WriteLine(
                    "Two independent Kestrel processes enforced one Redis limit across {0} concurrent requests.",
                    requestCount);
            }
            finally
            {
                foreach (var response in responses)
                {
                    response.Dispose();
                }
            }
        }
        finally
        {
            await Task.WhenAll(StopRateLimitingProbeHostAsync(firstHost), StopRateLimitingProbeHostAsync(secondHost));
        }
    }

    private static int GetFreeTcpPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private RateLimitingProbeHost StartRateLimitingProbeHost(int port, int requestLimit)
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent?.Name ?? "Debug";
        var probeHostDirectory = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "RateLimitingProbeHost",
            "bin",
            configuration,
            "net10.0"));
        var assemblyPath = Path.Combine(probeHostDirectory, ProbeHostAssemblyFileName);
        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException("The rate-limiting probe host was not built with the integration tests.", assemblyPath);
        }

        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = probeHostDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(ProbeHostAssemblyFileName);
        startInfo.Environment["GAMEGUILD_RATE_LIMIT_REDIS_ENDPOINT"] = fixture.RedisEndpoint;
        startInfo.Environment["GAMEGUILD_RATE_LIMIT_HTTP_PORT"] = port.ToString(System.Globalization.CultureInfo.InvariantCulture);
        startInfo.Environment["GAMEGUILD_RATE_LIMIT_REQUEST_LIMIT"] = requestLimit.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var process = Process.Start(startInfo) // NOSONAR: fixed executable and assembly argument; runtime settings are environment variables.
            ?? throw new InvalidOperationException("Failed to start a rate-limiting probe process.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        return new RateLimitingProbeHost(
            process,
            new Uri($"http://127.0.0.1:{port}"),
            standardOutput,
            standardError);
    }

    private static async Task WaitForProbeHostAsync(RateLimitingProbeHost host)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1) };
        var timeout = Stopwatch.StartNew();
        while (timeout.Elapsed < TimeSpan.FromSeconds(20))
        {
            if (host.Process.HasExited)
            {
                var error = await host.StandardError;
                var output = await host.StandardOutput;
                throw new InvalidOperationException($"Rate-limiting probe exited before readiness.\n{output}\n{error}");
            }

            try
            {
                using var response = await client.GetAsync(new Uri(host.BaseAddress, "/healthz"));
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // The child is still binding its Kestrel listener.
            }
            catch (TaskCanceledException)
            {
                // A short readiness request timed out; retry until the overall deadline.
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("Rate-limiting probe did not become ready within 20 seconds.");
    }

    private static async Task StopRateLimitingProbeHostAsync(RateLimitingProbeHost host)
    {
        if (!host.Process.HasExited)
        {
            host.Process.Kill(entireProcessTree: true);
        }
        await host.Process.WaitForExitAsync();
        await Task.WhenAll(host.StandardOutput, host.StandardError);
        host.Process.Dispose();
    }

    private sealed record RateLimitingProbeHost(
        Process Process,
        Uri BaseAddress,
        Task<string> StandardOutput,
        Task<string> StandardError);

    private static TimeSpan Percentile(IEnumerable<TimeSpan> latencies, double percentile)
    {
        var ordered = latencies.OrderBy(latency => latency).ToArray();
        var index = Math.Clamp((int)Math.Ceiling(ordered.Length * percentile) - 1, 0, ordered.Length - 1);
        return ordered[index];
    }

    [Fact]
    public async Task EndpointMiddlewareSharesTheGlobalLimitAcrossIndependentRedisConnections()
    {
        const int requestLimit = 25;
        var userId = Guid.NewGuid().ToString("N");
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = requestLimit;
        options.Period = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];

        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var responses = await Task.WhenAll(Enumerable.Range(0, 200).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                userId,
                endpoint: null)));

        limiterLog.Errors.Should().BeEmpty();
        responses.Count(response => response.StatusCode == StatusCodes.Status204NoContent).Should().Be(requestLimit);
        responses.Count(response => response.StatusCode == StatusCodes.Status429TooManyRequests).Should().Be(200 - requestLimit);

        var rejected = responses.First(response => response.StatusCode == StatusCodes.Status429TooManyRequests);
        rejected.Response.ContentType.Should().StartWith("application/problem+json");
        rejected.Response.Headers.RetryAfter.Should().NotBeEmpty();
        rejected.Response.Headers["RateLimit-Limit"].ToString().Should().Be(requestLimit.ToString());
        rejected.Response.Headers["RateLimit-Remaining"].ToString().Should().Be("0");
    }

    [Fact]
    public async Task EndpointMiddlewareSharesNamedPerIpPolicyAcrossIndependentRedisConnections()
    {
        const int policyLimit = 4;
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 100;
        options.Period = TimeSpan.FromMinutes(1);
        options.IpRequestsPerMinute = policyLimit;
        options.IpWindow = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];

        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.PerIp)),
            "per-ip-rate-limited");
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var responses = await Task.WhenAll(Enumerable.Range(0, 40).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                Guid.NewGuid().ToString("N"),
                endpoint)));

        limiterLog.Errors.Should().BeEmpty();
        responses.Count(response => response.StatusCode == StatusCodes.Status204NoContent).Should().Be(policyLimit);
        responses.Count(response => response.StatusCode == StatusCodes.Status429TooManyRequests).Should().Be(40 - policyLimit);
    }

    [Fact]
    public async Task EndpointMiddlewareSharesFixedAuthorizationLimitForResolvedTenantAcrossConnections()
    {
        const int policyLimit = 2;
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 100;
        options.Period = TimeSpan.FromMinutes(1);
        options.AuthorizationRequestsPerMinute = policyLimit;
        options.AuthorizationWindow = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.Authorization)),
            "tenant-authorization-rate-limited");
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var userId = Guid.NewGuid().ToString("N");
        var tenantId = Guid.NewGuid();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                userId,
                endpoint,
                resolvedTenantId: tenantId)));

        limiterLog.Errors.Should().BeEmpty();
        responses.Count(response => response.StatusCode == StatusCodes.Status204NoContent).Should().Be(policyLimit);
        responses.Count(response => response.StatusCode == StatusCodes.Status429TooManyRequests).Should().Be(20 - policyLimit);
    }

    [Fact]
    public async Task EndpointMiddlewareRecordsLowCardinalityRedisRejectionMetrics()
    {
        var measurements = new ConcurrentQueue<(long Value, string? Policy, string? Enforcement, string[] TagNames)>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == "GameGuild.API.RateLimiting" &&
                instrument.Name == "gameguild.api.rate_limit.rejections")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            string? policy = null;
            string? enforcement = null;
            var tagNames = new List<string>();
            foreach (var tag in tags)
            {
                tagNames.Add(tag.Key);
                if (tag.Key == "policy")
                {
                    policy = tag.Value?.ToString();
                }
                else if (tag.Key == "enforcement")
                {
                    enforcement = tag.Value?.ToString();
                }
            }

            measurements.Enqueue((value, policy, enforcement, tagNames.ToArray()));
        });
        listener.Start();

        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 10;
        options.Period = TimeSpan.FromMinutes(1);
        options.IpRequestsPerMinute = 1;
        options.IpWindow = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.PerIp)),
            "per-ip-metrics");
        var middleware = new RedisEndpointRateLimitingMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var logger = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var userId = Guid.NewGuid().ToString("N");

        var responses = await Task.WhenAll(Enumerable.Range(0, 2).Select(index =>
            SendThroughMiddlewareAsync(
                middleware,
                fixture.Connections[index],
                logger,
                options,
                requestServices,
                userId,
                endpoint,
                remoteIpAddress: "192.0.2.21")));

        responses.Select(response => response.StatusCode).Should().Contain(StatusCodes.Status204NoContent);
        responses.Select(response => response.StatusCode).Should().Contain(StatusCodes.Status429TooManyRequests);
        measurements.Should().Contain(measurement =>
            measurement.Value == 1 &&
            measurement.Policy == RateLimitPolicies.PerIp &&
            measurement.Enforcement == "redis");
        measurements.Should().OnlyContain(measurement =>
            (measurement.Policy == "global" || measurement.Policy == RateLimitPolicies.PerIp) &&
            measurement.Enforcement == "redis" &&
            measurement.TagNames.Length == 2 &&
            measurement.TagNames.Contains("policy") &&
            measurement.TagNames.Contains("enforcement"));
    }

    [Fact]
    public async Task EndpointMiddlewareSkipsGlobalAndNamedPoliciesForAllowlistedUsers()
    {
        var userId = Guid.NewGuid().ToString("N");
        var accessOptions = new RateLimitAccessOptions { AllowlistedUserIds = [userId] };
        accessOptions.Validate();
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 1;
        options.Period = TimeSpan.FromMinutes(1);
        options.AuthenticationRequestsPerMinute = 1;
        options.AuthenticationWindow = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.Authentication)),
            "authentication-rate-limited");
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                userId,
                endpoint,
                accessOptions: accessOptions)));

        limiterLog.Errors.Should().BeEmpty();
        responses.Should().OnlyContain(response => response.StatusCode == StatusCodes.Status204NoContent);
    }

    [Fact]
    public async Task TokenBucketAdmissionIsAtomicAcrossConnectionsAndReplenishes()
    {
        const int capacity = 6;
        const int refillTokens = 2;
        var period = TimeSpan.FromMilliseconds(250);
        var log = new CapturingLogger<RedisDistributedRateLimiter>();
        var limiters = fixture.Connections
            .Select(connection => new RedisDistributedRateLimiter(connection, log))
            .ToArray();

        var initial = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ =>
            limiters[Random.Shared.Next(limiters.Length)].TryAcquireTokenBucketAsync(
                "integration:shared-token-bucket",
                capacity,
                refillTokens,
                period)));

        initial.Count(decision => decision.IsAllowed).Should().Be(capacity);
        initial.Where(decision => !decision.IsAllowed).Should().OnlyContain(decision => decision.RetryAfter > TimeSpan.Zero);

        await Task.Delay(TimeSpan.FromMilliseconds(300));
        var refilled = await Task.WhenAll(Enumerable.Range(0, 3).Select(index =>
            limiters[index % limiters.Length].TryAcquireTokenBucketAsync(
                "integration:shared-token-bucket",
                capacity,
                refillTokens,
                period)));

        refilled.Count(decision => decision.IsAllowed).Should().Be(refillTokens);
        refilled.Single(decision => !decision.IsAllowed).RetryAfter.Should().BeGreaterThan(TimeSpan.Zero);
        log.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task EndpointMiddlewareSharesBurstyTokenBucketAcrossConnections()
    {
        const int tokenLimit = 5;
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 100;
        options.Period = TimeSpan.FromMinutes(1);
        options.TokenBucketLimit = tokenLimit;
        options.TokensPerPeriod = 1;
        options.TokenReplenishmentPeriod = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.Bursty)),
            "bursty-rate-limited");
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();
        var userId = Guid.NewGuid().ToString("N");

        var responses = await Task.WhenAll(Enumerable.Range(0, 30).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                userId,
                endpoint)));

        limiterLog.Errors.Should().BeEmpty();
        responses.Count(response => response.StatusCode == StatusCodes.Status204NoContent).Should().Be(tokenLimit);
        responses.Count(response => response.StatusCode == StatusCodes.Status429TooManyRequests).Should().Be(30 - tokenLimit);
    }

    [Fact]
    public async Task EndpointMiddlewareUsesPremiumApiKeyTokenBucketLimit()
    {
        const int premiumLimit = 3;
        var apiKeyId = Guid.NewGuid().ToString("N");
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 100;
        options.Period = TimeSpan.FromMinutes(1);
        options.StandardApiKeyRequestsPerMinute = 1;
        options.PremiumApiKeyRequestsPerMinute = premiumLimit;
        options.ApiKeyWindow = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.ApiKey)),
            "api-key-rate-limited");
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middlewares = fixture.Connections
            .Select(_ => new RedisEndpointRateLimitingMiddleware(next))
            .ToArray();
        var limiterLog = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var responses = await Task.WhenAll(Enumerable.Range(0, 30).Select(index =>
            SendThroughMiddlewareAsync(
                middlewares[index % middlewares.Length],
                fixture.Connections[index % fixture.Connections.Count],
                limiterLog,
                options,
                requestServices,
                Guid.NewGuid().ToString("N"),
                endpoint,
                apiKeyId: apiKeyId,
                apiKeyTier: "premium")));

        limiterLog.Errors.Should().BeEmpty();
        responses.Count(response => response.StatusCode == StatusCodes.Status204NoContent).Should().Be(premiumLimit);
        responses.Count(response => response.StatusCode == StatusCodes.Status429TooManyRequests).Should().Be(30 - premiumLimit);
    }

    [Fact]
    public async Task ConcurrentInstancesNeverAdmitMoreThanTheConfiguredSlidingWindowLimit()
    {
        const int requestLimit = 25;
        const int concurrentRequests = 200;
        var logger = new CapturingLogger<RedisDistributedRateLimiter>();
        var limiters = fixture.Connections
            .Select(connection => new RedisDistributedRateLimiter(
                connection,
                logger))
            .ToArray();

        var decisions = await Task.WhenAll(Enumerable.Range(0, concurrentRequests)
            .Select(index => limiters[index % limiters.Length]
                .IsAllowedAsync("integration:shared-actor", requestLimit, TimeSpan.FromSeconds(30))));

        logger.Errors.Should().BeEmpty();
        decisions.Count(allowed => allowed).Should().Be(requestLimit);
        (await limiters[0].GetCurrentCountAsync("integration:shared-actor", TimeSpan.FromSeconds(30)))
            .Should().Be(requestLimit);
        (await limiters[1].IsAllowedAsync("integration:shared-actor", requestLimit, TimeSpan.FromSeconds(30)))
            .Should().BeFalse();
    }

    [Fact]
    public async Task SlidingWindowEntriesExpireAndAllowNewRequests()
    {
        var logger = new CapturingLogger<RedisDistributedRateLimiter>();
        var limiter = new RedisDistributedRateLimiter(
            fixture.Connections[0],
            logger);

        (await limiter.IsAllowedAsync("integration:expiring-actor", 1, TimeSpan.FromMilliseconds(250)))
            .Should().BeTrue();
        (await limiter.IsAllowedAsync("integration:expiring-actor", 1, TimeSpan.FromMilliseconds(250)))
            .Should().BeFalse();

        await Task.Delay(TimeSpan.FromMilliseconds(350));

        (await limiter.IsAllowedAsync("integration:expiring-actor", 1, TimeSpan.FromMilliseconds(250)))
            .Should().BeTrue();
        logger.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExpensiveOperationConcurrencyIsSharedRenewedAndReleasedAcrossConnections()
    {
        var userId = Guid.NewGuid().ToString("N");
        var options = RateLimitingOptions.CreateDefault();
        options.Limit = 100;
        options.Period = TimeSpan.FromMinutes(1);
        options.MaxConcurrentRequests = 1;
        options.ConcurrencyLeaseDuration = TimeSpan.FromMilliseconds(180);
        options.ExemptPaths = [];
        var endpoint = new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(new EnableRateLimitingAttribute(RateLimitPolicies.ExpensiveOperations)),
            "expensive-operation");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var executed = 0;
        var next = new RequestDelegate(async context =>
        {
            Interlocked.Increment(ref executed);
            entered.TrySetResult();
            await release.Task;
            context.Response.StatusCode = StatusCodes.Status204NoContent;
        });
        var middleware = new RedisEndpointRateLimitingMiddleware(next);
        var logger = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var firstRequest = SendThroughMiddlewareAsync(
            middleware,
            fixture.Connections[0],
            logger,
            options,
            requestServices,
            userId,
            endpoint);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Task<MiddlewareResponse>? secondRequest = null;
        MiddlewareResponse? secondResponse = null;
        try
        {
            // The first operation outlives the initial lease TTL. Its renewal must
            // keep the shared slot occupied while it is still running.
            await Task.Delay(TimeSpan.FromMilliseconds(450));
            secondRequest = SendThroughMiddlewareAsync(
                middleware,
                fixture.Connections[1],
                logger,
                options,
                requestServices,
                userId,
                endpoint);
            secondResponse = await secondRequest.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            release.TrySetResult();
        }

        var firstResponse = await firstRequest.WaitAsync(TimeSpan.FromSeconds(5));
        if (secondRequest is not null)
        {
            secondResponse ??= await secondRequest.WaitAsync(TimeSpan.FromSeconds(5));
        }

        firstResponse.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        secondResponse!.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        secondResponse.Response.Headers.RetryAfter.Should().NotBeEmpty();

        var afterRelease = await SendThroughMiddlewareAsync(
            middleware,
            fixture.Connections[2],
            logger,
            options,
            requestServices,
            userId,
            endpoint);
        afterRelease.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        executed.Should().Be(2);
        logger.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ExpensiveOperationConcurrencyPrunesExpiredLeasesBeforeAdmission()
    {
        var userId = Guid.NewGuid().ToString("N");
        var redisKey = $"ratelimit:concurrency:api:concurrency:{RateLimitPolicies.ExpensiveOperations}:user:{userId}";
        var database = fixture.Connections[0].GetDatabase();
        await database.SortedSetAddAsync(
            redisKey,
            "abandoned-process",
            DateTimeOffset.UtcNow.AddSeconds(-1).ToUnixTimeMilliseconds());

        var limiter = new RedisDistributedRateLimiter(
            fixture.Connections[1],
            new CapturingLogger<RedisDistributedRateLimiter>());
        var decision = await limiter.TryAcquireConcurrencyLeaseAsync(
            $"api:concurrency:{RateLimitPolicies.ExpensiveOperations}:user:{userId}",
            maxConcurrent: 1,
            leaseDuration: TimeSpan.FromSeconds(2));

        decision.IsAllowed.Should().BeTrue();
        decision.Lease.Should().NotBeNull();
        await decision.Lease!.DisposeAsync();
        (await database.SortedSetLengthAsync(redisKey)).Should().Be(0);
    }

    [Fact]
    public async Task ProgressivePenaltiesEscalateAcrossRedisConnectionsAndDecay()
    {
        var log = new CapturingLogger<RedisDistributedRateLimiter>();
        var firstConnectionLimiter = new RedisDistributedRateLimiter(fixture.Connections[0], log);
        var secondConnectionLimiter = new RedisDistributedRateLimiter(fixture.Connections[1], log);
        var key = $"integration:penalty:{Guid.NewGuid():N}";

        var firstViolation = await firstConnectionLimiter.RecordRateLimitViolationAsync(
            key,
            violationThreshold: 2,
            decayWindow: TimeSpan.FromSeconds(5),
            basePenalty: TimeSpan.FromMilliseconds(120),
            maxPenalty: TimeSpan.FromMilliseconds(480));
        var secondViolation = await secondConnectionLimiter.RecordRateLimitViolationAsync(
            key,
            violationThreshold: 2,
            decayWindow: TimeSpan.FromSeconds(5),
            basePenalty: TimeSpan.FromMilliseconds(120),
            maxPenalty: TimeSpan.FromMilliseconds(480));

        firstViolation.Should().BeNull();
        secondViolation.Should().Be(TimeSpan.FromMilliseconds(120));
        (await firstConnectionLimiter.GetActivePenaltyAsync(key)).Should().BeGreaterThan(TimeSpan.Zero);

        await Task.Delay(TimeSpan.FromMilliseconds(160));
        (await secondConnectionLimiter.GetActivePenaltyAsync(key)).Should().BeNull();
        var escalatedViolation = await firstConnectionLimiter.RecordRateLimitViolationAsync(
            key,
            violationThreshold: 2,
            decayWindow: TimeSpan.FromSeconds(5),
            basePenalty: TimeSpan.FromMilliseconds(120),
            maxPenalty: TimeSpan.FromMilliseconds(480));

        escalatedViolation.Should().Be(TimeSpan.FromMilliseconds(240));
        log.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task EndpointMiddlewareEnforcesTemporaryPenaltyBeforeRateLimitPolicies()
    {
        var userId = Guid.NewGuid().ToString("N");
        var options = RateLimitingOptions.CreateDefault();
        options.EnableProgressivePenalties = true;
        options.PenaltyViolationThreshold = 1;
        options.PenaltyDecayWindow = TimeSpan.FromMinutes(1);
        options.PenaltyBaseDuration = TimeSpan.FromSeconds(2);
        options.PenaltyMaxDuration = TimeSpan.FromSeconds(2);
        options.Limit = 1;
        options.Period = TimeSpan.FromMinutes(1);
        options.ExemptPaths = [];
        var next = new RequestDelegate(context =>
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });
        var middleware = new RedisEndpointRateLimitingMiddleware(next);
        var logger = new CapturingLogger<RedisDistributedRateLimiter>();
        using var requestServices = new ServiceCollection().AddLogging().BuildServiceProvider();

        var accepted = await SendThroughMiddlewareAsync(
            middleware, fixture.Connections[0], logger, options, requestServices, userId, endpoint: null);
        var firstRejection = await SendThroughMiddlewareAsync(
            middleware, fixture.Connections[1], logger, options, requestServices, userId, endpoint: null);
        var penaltyRejection = await SendThroughMiddlewareAsync(
            middleware, fixture.Connections[2], logger, options, requestServices, userId, endpoint: null);

        accepted.StatusCode.Should().Be(StatusCodes.Status204NoContent);
        firstRejection.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        penaltyRejection.StatusCode.Should().Be(StatusCodes.Status429TooManyRequests);
        penaltyRejection.Response.ContentType.Should().StartWith("application/problem+json");
        penaltyRejection.Response.Headers.RetryAfter.Should().NotBeEmpty();
        logger.Errors.Should().BeEmpty();
    }

    private static async Task<IHost> StartRateLimitedTestHostAsync(
        IConnectionMultiplexer connection,
        RateLimitingOptions options)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .ConfigureServices(services =>
                {
                    services.AddLogging();
                    services.AddRouting();
                    var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager
                    {
                        ["Redis:Enabled"] = "true"
                    };
                    services.SetupRateLimiting(configuration, options);
                    services.AddSingleton<IDistributedRateLimiter>(provider => new RedisDistributedRateLimiter(
                        connection,
                        provider.GetRequiredService<ILogger<RedisDistributedRateLimiter>>()));
                })
                .Configure(app =>
                {
                    app.UseRouting();
                    app.Use(async (context, next) =>
                    {
                        if (context.Request.Headers.TryGetValue("X-Test-User", out var userId) &&
                            !string.IsNullOrWhiteSpace(userId))
                        {
                            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                                [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
                                "rate-limit-integration-test"));
                        }

                        await next(context).ConfigureAwait(false);
                    });
                    app.UseRateLimiter();
                    app.UseMiddleware<RedisEndpointRateLimitingMiddleware>();
                    app.UseEndpoints(endpoints => endpoints.MapGet("/limited", () => Results.NoContent()));
                }))
            .Build();

        await host.StartAsync().ConfigureAwait(false);
        return host;
    }

    private static async Task<MiddlewareResponse> SendThroughMiddlewareAsync(
        RedisEndpointRateLimitingMiddleware middleware,
        IConnectionMultiplexer connection,
        CapturingLogger<RedisDistributedRateLimiter> logger,
        RateLimitingOptions options,
        IServiceProvider requestServices,
        string userId,
        Endpoint? endpoint,
        string remoteIpAddress = "192.0.2.20",
        Guid? resolvedTenantId = null,
        RateLimitAccessOptions? accessOptions = null,
        string? apiKeyId = null,
        string? apiKeyTier = null)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = requestServices,
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)],
                "integration-test"))
        };
        if (apiKeyId is not null)
        {
            context.User.AddIdentity(new ClaimsIdentity(
            [
                new Claim("auth_method", "api_key"),
                new Claim("api_key_id", apiKeyId),
                new Claim("api_key_tier", apiKeyTier ?? "standard")
            ], "api-key"));
        }
        context.Request.Path = "/limited";
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIpAddress);
        if (resolvedTenantId.HasValue)
        {
            context.Items[HttpContextKeys.AuthorizationTenantId] = resolvedTenantId.Value;
        }
        context.SetEndpoint(endpoint);

        await middleware.InvokeAsync(
            context,
            new RedisDistributedRateLimiter(connection, logger),
            options,
            accessOptions ?? new RateLimitAccessOptions());

        return new MiddlewareResponse(context.Response.StatusCode, context.Response);
    }

    private sealed record MiddlewareResponse(int StatusCode, HttpResponse Response);

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public ConcurrentQueue<Exception> Errors { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            _ = state;
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            _ = logLevel;
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _ = logLevel;
            _ = eventId;
            _ = state;
            _ = formatter;
            if (exception is not null)
            {
                Errors.Enqueue(exception);
            }
        }
    }
}
