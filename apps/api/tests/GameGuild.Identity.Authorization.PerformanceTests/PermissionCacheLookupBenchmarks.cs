using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GameGuild.Identity.Authorization.PerformanceTests;

/// <summary>
/// Measures the local L1 permission-cache lookup path against a direct in-memory
/// authorization-store lookup. This isolates cache overhead; it is not an API or database benchmark.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob]
public class PermissionCacheLookupBenchmarks
{
    private readonly ConcurrentDictionary<string, bool> _authoritativePermissions = new(StringComparer.Ordinal);
    private MemoryCache _memoryCache = null!;
    private HybridPermissionCache _permissionCache = null!;
    private string[] _keys = [];
    private int _nextKey;

    [Params(1, 256, 4096)]
    public int WorkingSetSize { get; set; }

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 5000 });
        _permissionCache = new HybridPermissionCache(
            _memoryCache,
            Options.Create(new AuthorizationCacheOptions
            {
                UseDistributedCache = false,
                PermissionTtlSeconds = 300,
                MaxL1CacheSize = 5000
            }),
            new CacheMetricsService(),
            NullLogger<HybridPermissionCache>.Instance);

        var tenantId = Guid.Parse("d28d4a2e-df59-4f73-bbb0-4048f42b693a");
        var userId = Guid.Parse("fdaf2ac9-0994-47ae-9b48-208d550bec1d");
        _keys = Enumerable.Range(0, WorkingSetSize)
            .Select(index => $"acl:{tenantId}:{userId}:Document:benchmark-{index}:tv1:uv1")
            .ToArray();

        foreach (var key in _keys)
        {
            _authoritativePermissions[key] = true;
            await _permissionCache.SetValueAsync(key, true, "acl").ConfigureAwait(false);
        }
    }

    [Benchmark(Baseline = true, Description = "Direct in-memory authorization lookup")]
    public bool DirectAuthorizationLookup() =>
        _authoritativePermissions.TryGetValue(NextKey(), out var allowed) && allowed;

    [Benchmark(Description = "Hybrid permission cache L1 hit")]
    public async Task<bool> PermissionCacheL1HitAsync()
    {
        var result = await _permissionCache
            .GetValueAsync<bool>(NextKey(), "acl")
            .ConfigureAwait(false);

        return result.Found && result.Value;
    }

    [GlobalCleanup]
    public void Cleanup() => _memoryCache.Dispose();

    private string NextKey()
    {
        var index = Interlocked.Increment(ref _nextKey);
        return _keys[(int)((uint)index % (uint)_keys.Length)];
    }
}
