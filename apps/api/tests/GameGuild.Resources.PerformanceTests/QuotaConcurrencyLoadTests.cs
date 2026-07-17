using System.Collections.Concurrent;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Resources.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using Xunit;

namespace GameGuild.Resources.PerformanceTests;

/// <summary>
/// Load tests for resource quota operations verifying:
/// - 1000+ concurrent quota operations
/// - Atomic consumption under contention
/// - Throughput and latency metrics
/// 
/// From: ASSETS_RESOURCES_DEEP_REVIEW.md 60-Day Sprint - Week 8
/// </summary>
[Trait("Category", "Performance")]
[Trait("LoadTest", "QuotaOperations")]
[Collection(QuotaConcurrencyPostgreSqlCollection.Name)]
public class QuotaConcurrencyLoadTests(PostgreSqlTestFixture fixture)
{
    private readonly Guid _testTenantId = Guid.NewGuid();

    /// <summary>
    /// Performance test: 1000 concurrent atomic consume operations.
    /// Verifies that quota limits are enforced under high contention.
    /// </summary>
    [Theory]
    [InlineData(100, 10)]   // 100 concurrent, 10 limit
    [InlineData(500, 50)]   // 500 concurrent, 50 limit
    [InlineData(1000, 100)] // 1000 concurrent, 100 limit - Primary test case
    public async Task ConcurrentAtomicConsume_EnforcesHardLimit_UnderLoad(int concurrentRequests, int hardLimit)
    {
        // Arrange
        await using var database = await fixture.CreateDatabaseAsync("quota_load");
        var connectionString = CreatePooledConnectionString(database.ConnectionString);

        // Set up quota with hard limit
        await SetQuotaAsync(connectionString, _testTenantId, ResourceUsageType.ApiCalls, hardLimit - 10, hardLimit);

        var successCount = 0;
        var failureCount = 0;
        var exceptions = new ConcurrentQueue<Exception>();
        var stopwatch = Stopwatch.StartNew();

        // Act - Fire concurrent requests
        var tasks = Enumerable.Range(0, concurrentRequests)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    var success = await ConsumeAsync(connectionString, _testTenantId, ResourceUsageType.ApiCalls);

                    if (success)
                        Interlocked.Increment(ref successCount);
                    else
                        Interlocked.Increment(ref failureCount);
                }
                catch (Exception exception)
                {
                    exceptions.Enqueue(exception);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        exceptions.Should().BeEmpty("concurrent quota rejection must not be represented by infrastructure exceptions");
        successCount.Should().Be(hardLimit,
            $"exactly the hard limit of {hardLimit} should be consumable under {concurrentRequests} concurrent requests");
        failureCount.Should().Be(concurrentRequests - hardLimit);

        var totalProcessed = successCount + failureCount;
        totalProcessed.Should().Be(concurrentRequests,
            "All requests should be processed (success or failure)");

        // Performance metrics
        var throughput = concurrentRequests / stopwatch.Elapsed.TotalSeconds;
        var avgLatencyMs = stopwatch.Elapsed.TotalMilliseconds / concurrentRequests;

        // Log performance metrics for baseline establishment
        Console.WriteLine($"=== Quota Load Test Results ===");
        Console.WriteLine($"Concurrent Requests: {concurrentRequests}");
        Console.WriteLine($"Hard Limit: {hardLimit}");
        Console.WriteLine($"Successful Consumes: {successCount}");
        Console.WriteLine($"Failed (Quota Exceeded): {failureCount}");
        Console.WriteLine($"Total Duration: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Throughput: {throughput:F2} ops/sec");
        Console.WriteLine($"Avg Latency: {avgLatencyMs:F4}ms per operation");
    }

    /// <summary>
    /// Tests that concurrent quota checks for different tenants don't interfere.
    /// </summary>
    [Fact]
    public async Task ConcurrentQuotaChecks_MultiTenant_NoInterference()
    {
        // Arrange
        await using var database = await fixture.CreateDatabaseAsync("quota_multi_tenant");
        var connectionString = CreatePooledConnectionString(database.ConnectionString);

        // Create 10 tenants with different quotas
        var tenants = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToArray();
        foreach (var tenantId in tenants)
        {
            await SetQuotaAsync(connectionString, tenantId, ResourceUsageType.Storage, 90, 100);
        }

        var tenantSuccessCounts = new ConcurrentDictionary<Guid, int>();
        var exceptions = new ConcurrentQueue<Exception>();
        var stopwatch = Stopwatch.StartNew();

        // Act - 100 concurrent requests per tenant (1000 total)
        var tasks = tenants.SelectMany(tenantId =>
            Enumerable.Range(0, 100).Select(_ => Task.Run(async () =>
            {
                try
                {
                    var success = await ConsumeAsync(connectionString, tenantId, ResourceUsageType.Storage);

                    if (success)
                        tenantSuccessCounts.AddOrUpdate(tenantId, 1, (_, count) => count + 1);
                }
                catch (Exception exception)
                {
                    exceptions.Enqueue(exception);
                }
            })))
            .ToArray();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert - Each tenant should have exactly their limit consumed
        exceptions.Should().BeEmpty("tenants must not interfere through shared EF state or database errors");
        foreach (var tenantId in tenants)
        {
            var successCount = tenantSuccessCounts.GetValueOrDefault(tenantId, 0);
            successCount.Should().Be(100,
                $"Tenant {tenantId} should consume exactly its independent hard limit of 100");
        }

        Console.WriteLine($"=== Multi-Tenant Load Test Results ===");
        Console.WriteLine($"Total Tenants: {tenants.Length}");
        Console.WriteLine($"Requests per Tenant: 100");
        Console.WriteLine($"Total Requests: 1000");
        Console.WriteLine($"Duration: {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Throughput: {1000 / stopwatch.Elapsed.TotalSeconds:F2} ops/sec");
    }

    /// <summary>
    /// Stress test: Burst of requests followed by cooldown pattern.
    /// </summary>
    [Fact]
    public async Task BurstPattern_QuotaEnforcement_Stable()
    {
        // Arrange
        await using var database = await fixture.CreateDatabaseAsync("quota_burst");
        var connectionString = CreatePooledConnectionString(database.ConnectionString);
        await SetQuotaAsync(connectionString, _testTenantId, ResourceUsageType.Projects, 40, 50);

        var totalSuccess = 0;
        var burstResults = new List<(int BurstNumber, int SuccessCount, double DurationMs)>();
        var exceptions = new ConcurrentQueue<Exception>();

        // Act - 5 bursts of 200 requests each
        for (int burst = 0; burst < 5; burst++)
        {
            var burstStopwatch = Stopwatch.StartNew();
            var burstSuccess = 0;

            var tasks = Enumerable.Range(0, 200)
                .Select(_ => Task.Run(async () =>
                {
                    try
                    {
                        var success = await ConsumeAsync(connectionString, _testTenantId, ResourceUsageType.Projects);

                        if (success) Interlocked.Increment(ref burstSuccess);
                    }
                    catch (Exception exception)
                    {
                        exceptions.Enqueue(exception);
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);
            burstStopwatch.Stop();

            burstResults.Add((burst + 1, burstSuccess, burstStopwatch.Elapsed.TotalMilliseconds));
            totalSuccess += burstSuccess;

            // Cooldown between bursts
            await Task.Delay(50);
        }

        // Assert
        exceptions.Should().BeEmpty("burst rejection must be a quota result, not an infrastructure exception");
        totalSuccess.Should().Be(50,
            "all available quota should be consumed exactly once across bursts");

        Console.WriteLine($"=== Burst Pattern Test Results ===");
        foreach (var (burstNumber, successCount, durationMs) in burstResults)
        {
            Console.WriteLine($"Burst {burstNumber}: {successCount} successes in {durationMs:F2}ms");
        }
        Console.WriteLine($"Total Successes: {totalSuccess} (Limit: 50)");
    }

    private static ResourceQuotaService CreateResourceQuotaService(
        IResourceQuotaRepository repository,
        IUsageRecordRepository usageRepository,
        IPublisher publisher)
    {
        var management = new QuotaManagementService(
            repository,
            usageRepository,
            publisher,
            NullLogger<QuotaManagementService>.Instance);

        var enforcement = new QuotaEnforcementService(
            repository,
            management,
            publisher,
            NullLogger<QuotaEnforcementService>.Instance);

        var maintenance = new QuotaMaintenanceService(
            repository,
            usageRepository,
            management,
            publisher,
            NullLogger<QuotaMaintenanceService>.Instance);

        return new ResourceQuotaService(management, enforcement, maintenance);
    }

    private static async Task SetQuotaAsync(
        string connectionString,
        Guid tenantId,
        ResourceUsageType type,
        long softLimit,
        long hardLimit)
    {
        await using var scope = ResourceQuotaPostgreSqlScope.Create(connectionString);
        await scope.Context.Database.EnsureCreatedAsync();
        await scope.Service.SetQuotaAsync(tenantId, type, softLimit, hardLimit);
    }

    private static async Task<bool> ConsumeAsync(
        string connectionString,
        Guid tenantId,
        ResourceUsageType type)
    {
        await using var scope = ResourceQuotaPostgreSqlScope.Create(connectionString);
        var (success, _, _) = await scope.Service.TryAtomicConsumeAsync(tenantId, type, 1);
        return success;
    }

    private static string CreatePooledConnectionString(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Pooling = true,
            MaxPoolSize = 50,
            Timeout = 30,
            CommandTimeout = 60
        };

        return builder.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class QuotaConcurrencyPostgreSqlCollection : ICollectionFixture<PostgreSqlTestFixture>
{
    public const string Name = "ResourceQuotaPerformancePostgreSql";
}

/// <summary>
/// BenchmarkDotNet benchmarks for quota operations.
/// Run with: dotnet run -c Release -- --filter "*QuotaBenchmarks*"
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class QuotaOperationBenchmarks
{
    private ResourceQuotaService _service = null!;
    private ResourceQuotaTestDbContext _context = null!;
    private readonly Guid _tenantId = Guid.NewGuid();

    [GlobalSetup]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ResourceQuotaTestDbContext>()
            .UseInMemoryDatabase($"BenchmarkDb_{Guid.NewGuid()}")
            .Options;

        _context = new ResourceQuotaTestDbContext(options);
        var repository = new ResourceQuotaRepository(_context);
        var usageRepository = new UsageRecordRepository(_context);
        var publisherMock = new Mock<IPublisher>();

        var management = new QuotaManagementService(
            repository,
            usageRepository,
            publisherMock.Object,
            NullLogger<QuotaManagementService>.Instance);

        var enforcement = new QuotaEnforcementService(
            repository,
            management,
            publisherMock.Object,
            NullLogger<QuotaEnforcementService>.Instance);

        var maintenance = new QuotaMaintenanceService(
            repository,
            usageRepository,
            management,
            publisherMock.Object,
            NullLogger<QuotaMaintenanceService>.Instance);

        _service = new ResourceQuotaService(management, enforcement, maintenance);

        // Pre-create quota for benchmarks
        _service.SetQuotaAsync(_tenantId, ResourceUsageType.ApiCalls, 1000000, 10000000).Wait();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _context?.Dispose();
    }

    [Benchmark(Baseline = true)]
    public async Task GetQuota()
    {
        await _service.GetQuotaAsync(_tenantId, ResourceUsageType.ApiCalls);
    }

    [Benchmark]
    public async Task CheckLimits()
    {
        await _service.CheckLimitsAsync(_tenantId, ResourceUsageType.ApiCalls, 1);
    }

    [Benchmark]
    public async Task AtomicConsume()
    {
        await _service.TryAtomicConsumeAsync(_tenantId, ResourceUsageType.ApiCalls, 1);
    }

    [Benchmark]
    [Arguments(10)]
    [Arguments(100)]
    public async Task BatchCheckLimits(int batchSize)
    {
        var requests = Enumerable.Range(0, batchSize)
            .ToDictionary(_ => ResourceUsageType.ApiCalls, _ => 1L);

        await _service.CheckMultipleLimitsAsync(_tenantId, requests);
    }
}
