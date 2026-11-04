using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GameGuild.API;
using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce.Products;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Users;
using GameGuild.Social.Posts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.LoadSmoke;

/// <summary>
///     Hosted load smoke (readiness 2026-11 "operational readiness UNKNOWN"): boots the real API
///     host with the real JWT pipeline and a fully migrated PostgreSQL database (same fixture
///     family as the session-state acceptance runner), then drives K iterations of the five
///     hottest entry/read paths at concurrency N: local sign-in, social feed page, economy wallet
///     read, product browse, marketplace checkout initiation. This is NOT a load test — it is a
///     smoke that catches pathological cliffs (the missing-index / N+1 class that turns a path
///     into a table scan) before they ship. Assertions: zero 5xx and per-path p95 within budget.
///     A blown budget is issue material: attach the emitted load-smoke.json profile. The sign-in
///     budget is deliberately loose — real BCrypt-12 verification (~250-400 ms on shared runners)
///     is part of the measured path and the smoke must not train anyone to lower the work factor.
///     The authentication rate limiter is raised via in-memory configuration only (the 10/min
///     default would throttle the loop before any path is measured); limiter behavior itself is
///     covered by the dedicated security suites.
/// </summary>
internal static class Runner
{
    private const string SignIn = "sign-in";
    private const string Feed = "feed-page";
    private const string Wallet = "wallet-read";
    private const string Products = "product-browse";
    private const string Checkout = "checkout-initiation";
    private const string CartAdd = "cart-add-item"; // per-iteration precondition for checkout; asserted and loosely budgeted

    // Mirrors the internal ApiPostgreSqlTestAuthHandler constants in
    // GameGuild.API.IntegrationTests/Infrastructure/ApiPostgreSqlFixture.cs (the handler itself is
    // assembly-internal; the scheme contract is stable because the shared fixture owns it).
    private const string SyntheticScheme = "ApiPostgreSqlTest";
    private const string SyntheticUserIdHeader = "X-Test-User-Id";
    private const string SyntheticTenantIdHeader = "X-Test-Tenant-Id";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static readonly int Concurrency = ReadInt("LOAD_SMOKE_CONCURRENCY", 10);
    private static readonly int Iterations = ReadInt("LOAD_SMOKE_ITERATIONS", 25);
    private static readonly int ProductRows = ReadInt("LOAD_SMOKE_PRODUCT_ROWS", 200);
    private static readonly int PostsPerUser = ReadInt("LOAD_SMOKE_POSTS_PER_USER", 5);

    // p95 budgets (milliseconds). Cliff detectors, not SLOs: healthy paths on a shared CI runner
    // land far below these; only a pathological plan (seq scan, N+1, lock convoy) blows them.
    // Override per environment with LOAD_SMOKE_BUDGET_<KEY>.
    private static readonly Dictionary<string, int> BudgetsMs = new()
    {
        [SignIn] = ReadInt("LOAD_SMOKE_BUDGET_SIGN_IN", 4_000),
        [Feed] = ReadInt("LOAD_SMOKE_BUDGET_FEED", 1_500),
        [Wallet] = ReadInt("LOAD_SMOKE_BUDGET_WALLET", 1_500),
        [Products] = ReadInt("LOAD_SMOKE_BUDGET_PRODUCTS", 1_500),
        [Checkout] = ReadInt("LOAD_SMOKE_BUDGET_CHECKOUT", 3_000),
        [CartAdd] = ReadInt("LOAD_SMOKE_BUDGET_CART_ADD", 3_000),
    };

    public static async Task<int> Main(string[] args)
    {
        if ((Environment.GetEnvironmentVariable("GITHUB_ACTIONS") != "true"
             && Environment.GetEnvironmentVariable("LOAD_SMOKE_ALLOW_LOCAL") != "true")
            || args.Length != 2)
        {
            throw new InvalidOperationException(
                "Run this load-smoke harness only in its hosted workflow (or with LOAD_SMOKE_ALLOW_LOCAL=true), with repository and evidence paths.");
        }

        var repository = Path.GetFullPath(args[0]);
        var evidence = Path.GetFullPath(args[1]);
        var apiContentRoot = Path.Combine(repository, "apps", "api", "Source", "GameGuild.API");
        Assert.True(File.Exists(Path.Combine(apiContentRoot, "GameGuild.API.csproj")));
        Directory.CreateDirectory(evidence);

        var fixture = new ApiPostgreSqlFixture();
        try
        {
            await fixture.InitializeAsync();

            // Two real hosts over one migrated database: the real-JWT host serves sign-in; the
            // synthetic-authentication host serves the four authenticated paths with a stable
            // header-driven actor (the same fixture split the session-state acceptance uses).
            using var realAuthHost = fixture.RealAuthenticationFactory.WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(apiContentRoot);
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["RateLimiting:AuthenticationRequestsPerMinute"] = "10000" }));
            });
            using var syntheticHost = fixture.Factory.WithWebHostBuilder(builder =>
            {
                builder.UseContentRoot(apiContentRoot);
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                    new Dictionary<string, string?> { ["RateLimiting:AuthenticationRequestsPerMinute"] = "10000" }));
            });
            // Real sockets, not the in-memory TestServer handler: latency budgets must include
            // the actual Kestrel HTTP transport (same pattern as the session-state acceptance).
            realAuthHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            syntheticHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
            var realAuthAddress = ListenAddress(realAuthHost);
            var syntheticAddress = ListenAddress(syntheticHost);

            var marker = Guid.NewGuid().ToString("N");
            var accounts = await SeedAsync(syntheticHost.Services, marker);
            Console.WriteLine(
                $"Load smoke seeded {accounts.Count} accounts, {Math.Max(ProductRows, Concurrency)} products, {accounts.Count * PostsPerUser} posts.");

            using var realAuthClient = new HttpClient { BaseAddress = realAuthAddress };
            using var syntheticClient = new HttpClient { BaseAddress = syntheticAddress };

            // Warm-up: one full pass outside the measured window so first-hit JIT, connection
            // establishment and plan caching do not masquerade as cliffs.
            await RunPassAsync(realAuthClient, syntheticClient, accounts, warmUp: true);

            var measurements = await RunPassAsync(realAuthClient, syntheticClient, accounts, warmUp: false);

            var profiles = measurements.OrderBy(entry => entry.Key).Select(entry =>
            {
                var latencies = entry.Value.Select(sample => sample.Milliseconds).OrderBy(value => value).ToArray();
                return new
                {
                    path = entry.Key,
                    requests = latencies.Length,
                    p50Ms = Percentile(latencies, 0.50),
                    p95Ms = Percentile(latencies, 0.95),
                    maxMs = latencies.Length == 0 ? 0 : latencies[^1],
                    budgetMs = BudgetsMs[entry.Key],
                    statusCounts = entry.Value.GroupBy(sample => sample.StatusCode)
                        .ToDictionary(
                            group => ((int)group.Key).ToString(CultureInfo.InvariantCulture),
                            group => group.Count()),
                };
            }).ToArray();

            var failures = new List<string>();
            foreach (var group in measurements)
            {
                var errors = group.Value.Count(sample => (int)sample.StatusCode >= 500);
                if (errors > 0)
                {
                    failures.Add($"{group.Key}: {errors} request(s) returned 5xx");
                }

                var p95 = Percentile(group.Value.Select(sample => sample.Milliseconds).OrderBy(value => value).ToArray(), 0.95);
                if (p95 > BudgetsMs[group.Key])
                {
                    failures.Add($"{group.Key}: p95 {p95} ms exceeds budget {BudgetsMs[group.Key]} ms");
                }
            }

            var report = new
            {
                passed = failures.Count == 0,
                failures,
                profile = new
                {
                    concurrency = Concurrency,
                    iterations = Iterations,
                    warmUpPasses = 1,
                    productRows = Math.Max(ProductRows, Concurrency),
                    postsPerUser = PostsPerUser,
                    transport = "Actual Kestrel HTTP (loopback)",
                    authentication = "Real JWT pipeline (sign-in) + fixture synthetic actor (authenticated reads)",
                    database = "Migrated PostgreSQL (full EF migration history)",
                },
                profiles,
            };
            await File.WriteAllTextAsync(Path.Combine(evidence, "load-smoke.json"), JsonSerializer.Serialize(report, JsonOptions));
            Console.WriteLine(JsonSerializer.Serialize(report, JsonOptions));
            Console.WriteLine(failures.Count == 0
                ? $"Load smoke passed: {profiles.Length} paths x {Concurrency * Iterations} measured requests, zero 5xx, all p95 budgets held."
                : "Load smoke FAILED — see failures in load-smoke.json.");
            return failures.Count == 0 ? 0 : 1;
        }
        finally
        {
            await fixture.DisposeAsync();
        }
    }

    private static Uri ListenAddress(WebApplicationFactory<GameGuild.API.Program> factory)
    {
        var server = factory.Services.GetRequiredService<IServer>();
        Assert.StartsWith("Microsoft.AspNetCore.Server.Kestrel", server.GetType().Namespace!);
        var address = new Uri(Assert.Single(server.Features.Get<IServerAddressesFeature>()!.Addresses));
        Assert.True(address.IsLoopback && address.Scheme == "http" && address.Port > 0);
        return address;
    }

    private static async Task<ConcurrentDictionary<string, ConcurrentBag<Sample>>> RunPassAsync(
        HttpClient realAuthClient,
        HttpClient syntheticClient,
        IReadOnlyList<SeededAccount> accounts,
        bool warmUp)
    {
        var measurements = new ConcurrentDictionary<string, ConcurrentBag<Sample>>();
        var workers = Enumerable.Range(0, Concurrency).Select(async index =>
        {
            var account = accounts[index % accounts.Count];
            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                // 1. sign-in — real JWT pipeline, real BCrypt verification, per-account credentials.
                await MeasureAsync(measurements, SignIn, warmUp, async () =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/sign-in")
                    {
                        Content = Json(new { email = account.Email, password = account.Password }),
                    };
                    using var response = await realAuthClient.SendAsync(request);
                    return (response.StatusCode, await response.Content.ReadAsStringAsync());
                });

                // 2. feed page — real multi-query feed (blocks, mutes, follows, posts, sessions).
                await MeasureAsync(measurements, Feed, warmUp, () =>
                    SendAuthedAsync(syntheticClient, account, HttpMethod.Get, "/api/social/feed?scope=for-you&take=20"));

                // 3. wallet balance read.
                await MeasureAsync(measurements, Wallet, warmUp, () =>
                    SendAuthedAsync(syntheticClient, account, HttpMethod.Get, "/api/v1/economy/wallet"));

                // 4. product browse — paged, the classic missing-index surface.
                await MeasureAsync(measurements, Products, warmUp, () =>
                    SendAuthedAsync(syntheticClient, account, HttpMethod.Get,
                        $"/api/v1/products?skip={(iteration % 4) * 50}&take=50"));

                // 5. checkout initiation — a fresh cart item then checkout; the cart's terminal
                //    state means every iteration walks create-cart -> add -> checkout.
                var key = $"loadsmoke-{account.UserId:N}-{(warmUp ? "warm" : "run")}-{iteration}";
                var add = await MeasureAsync(measurements, CartAdd, warmUp, () =>
                    SendAuthedAsync(syntheticClient, account, HttpMethod.Post, "/api/v1/marketplace/cart/items",
                        Json(new
                        {
                            productId = account.CheckoutProduct.ProductId,
                            productPricingId = account.CheckoutProduct.PricingId,
                            productPricingVersionId = account.CheckoutProduct.VersionId,
                            quantity = 1,
                            idempotencyKey = key + ":add",
                        })));
                if ((int)add.StatusCode is >= 200 and < 300)
                {
                    var expectedVersion = JsonDocument.Parse(add.Body).RootElement.GetProperty("version").GetInt32();
                    await MeasureAsync(measurements, Checkout, warmUp, () =>
                        SendAuthedAsync(syntheticClient, account, HttpMethod.Post, "/api/v1/marketplace/cart/checkout",
                            Json(new { expectedVersion, idempotencyKey = key + ":checkout" })));
                }
            }
        });
        await Task.WhenAll(workers);
        return measurements;
    }

    private static async Task<(HttpStatusCode StatusCode, string Body)> SendAuthedAsync(
        HttpClient client,
        SeededAccount account,
        HttpMethod method,
        string path,
        HttpContent? content = null)
    {
        using var request = new HttpRequestMessage(method, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue(SyntheticScheme, "authenticated");
        request.Headers.Add(SyntheticUserIdHeader, account.UserId.ToString());
        request.Headers.Add(SyntheticTenantIdHeader, account.TenantId.ToString());
        request.Headers.Add("X-Tenant-Id", account.TenantId.ToString());
        using var response = await client.SendAsync(request);
        return (response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private static async Task<(HttpStatusCode StatusCode, string Body)> MeasureAsync(
        ConcurrentDictionary<string, ConcurrentBag<Sample>> measurements,
        string path,
        bool warmUp,
        Func<Task<(HttpStatusCode StatusCode, string Body)>> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        (HttpStatusCode statusCode, string body) result;
        try
        {
            result = await operation();
        }
        catch (Exception)
        {
            // Transport-level failure is the loudest cliff of all; record it, then fail the run.
            if (!warmUp)
            {
                measurements.GetOrAdd(path, _ => new ConcurrentBag<Sample>())
                    .Add(new Sample(stopwatch.ElapsedMilliseconds, HttpStatusCode.BadGateway));
            }
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
        if (!warmUp)
        {
            measurements.GetOrAdd(path, _ => new ConcurrentBag<Sample>())
                .Add(new Sample(stopwatch.ElapsedMilliseconds, result.statusCode));
        }
        return result;
    }

    private static async Task<List<SeededAccount>> SeedAsync(IServiceProvider hostServices, string marker)
    {
        await using var scope = hostServices.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Load smoke tenant " + marker,
            Slug = "loadsmoke-" + marker,
            AdminEmail = $"loadsmoke-{marker}@example.test",
            IsActive = true,
        };
        db.Add(tenant);

        // Product catalog: browse must scan a real result set, and checkout needs one fully
        // published product + immutable pricing + active version per account (PriceSnapshot
        // validates all three rows plus version parity before any order is written).
        var catalog = new List<(Guid ProductId, Guid PricingId, Guid VersionId)>();
        for (var index = 0; index < Math.Max(ProductRows, Concurrency); index++)
        {
            var product = Product.Create(
                "Load smoke product " + index + " " + marker,
                ProductType.Program,
                description: "Synthetic catalog row for the load smoke browse path.",
                tenantId: tenant.Id);
            var (pricing, version) = ProductPricing.CreateWithVersion(
                product.Id, "Base", 9.90m + (index % 40), salePrice: null, "USD", isDefault: true, tenant.Id);
            db.AddRange(product, pricing, version);
            catalog.Add((product.Id, pricing.Id, version.Id));
        }

        var password = "LoadSmoke-" + marker + "-Aa1!";
        var accounts = new List<SeededAccount>();
        for (var index = 0; index < Concurrency; index++)
        {
            var user = User.Create($"loadsmoke-{marker}-{index}@example.test", "Load smoke account " + index);
            user.Username = $"loadsmoke-{marker}-{index}";
            user.SetPasswordHash(passwordHasher.HashPassword(password));
            db.AddRange(user, new TenantMember
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TenantId = tenant.Id,
                Role = "Member",
                IsActive = true,
            });
            for (var post = 0; post < PostsPerUser; post++)
            {
                db.Add(Post.Create(user.Id, "Load smoke post " + post + " " + marker, PostVisibility.Public, tenant.Id));
            }
            accounts.Add(new SeededAccount(user.Id, user.Email, tenant.Id, password, catalog[index % catalog.Count]));
        }

        await db.SaveChangesAsync();
        return accounts;
    }

    private static HttpContent Json(object payload) =>
        new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    private static long Percentile(long[] sortedLatencies, double quantile)
    {
        if (sortedLatencies.Length == 0)
        {
            return 0;
        }
        var index = (int)Math.Ceiling(quantile * sortedLatencies.Length) - 1;
        return sortedLatencies[Math.Clamp(index, 0, sortedLatencies.Length - 1)];
    }

    private static int ReadInt(string name, int fallback) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) && value > 0 ? value : fallback;

    private sealed record SeededAccount(Guid UserId, string Email, Guid TenantId, string Password,
        (Guid ProductId, Guid PricingId, Guid VersionId) CheckoutProduct);

    private readonly record struct Sample(long Milliseconds, HttpStatusCode StatusCode);
}
