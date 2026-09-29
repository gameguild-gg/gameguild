using System.Text.Json;
using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheBulkOperationsTests
{
    [Fact]
    public async Task GetManyValuesAsync_DeduplicatesKeysReadsL2AndPromotesHitsToL1()
    {
        var distributed = new Mock<IDistributedCache>(MockBehavior.Strict);
        var payloads = new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            ["acl:read"] = JsonSerializer.SerializeToUtf8Bytes(AccessLevel.Read),
            ["acl:none"] = JsonSerializer.SerializeToUtf8Bytes(AccessLevel.None)
        };
        distributed
            .Setup(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns((string key, CancellationToken _) =>
                Task.FromResult(payloads.TryGetValue(key, out var payload) ? payload : null));

        using var memory = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1_000 });
        var metrics = new CacheMetricsService();
        var cache = CreateCache(memory, metrics, distributed.Object);

        var result = await cache.GetManyValuesAsync<AccessLevel>(
            ["acl:read", "acl:none", "acl:read", "acl:missing"],
            "acl");

        result.Should().HaveCount(3);
        result["acl:read"].Should().Be(CacheResult<AccessLevel>.Hit(AccessLevel.Read));
        result["acl:none"].Should().Be(CacheResult<AccessLevel>.Hit(AccessLevel.None));
        result["acl:missing"].Found.Should().BeFalse();
        distributed.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));

        var secondResult = await cache.GetManyValuesAsync<AccessLevel>(["acl:read", "acl:none", "acl:missing"], "acl");
        secondResult["acl:read"].Value.Should().Be(AccessLevel.Read);
        secondResult["acl:none"].Value.Should().Be(AccessLevel.None);
        distributed.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task SetManyValuesAsync_WritesEveryValueAndSubsequentReadsUseL1()
    {
        var distributed = new Mock<IDistributedCache>(MockBehavior.Strict);
        distributed
            .Setup(cache => cache.SetAsync(
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var memory = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1_000 });
        var metrics = new CacheMetricsService();
        var cache = CreateCache(memory, metrics, distributed.Object);
        var values = new Dictionary<string, AccessLevel>(StringComparer.Ordinal)
        {
            ["acl:1"] = AccessLevel.Read,
            ["acl:2"] = AccessLevel.Write,
            ["acl:3"] = AccessLevel.None
        };

        await cache.SetManyValuesAsync(values, "acl");

        distributed.Verify(cache => cache.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()), Times.Exactly(values.Count));

        var readBack = await cache.GetManyValuesAsync<AccessLevel>(values.Keys.ToArray(), "acl");
        readBack.Should().HaveCount(values.Count);
        foreach (var (key, expected) in values)
        {
            readBack[key].Should().Be(CacheResult<AccessLevel>.Hit(expected));
        }

        distributed.Verify(cache => cache.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task BulkOperations_RejectOversizedAndInvalidInputs()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions { SizeLimit = 1_000 });
        var metrics = new CacheMetricsService();
        var cache = CreateCache(memory, metrics, distributedCache: null);
        var tooManyKeys = Enumerable.Range(0, 501).Select(index => $"acl:{index}").ToArray();
        var tooManyValues = tooManyKeys.ToDictionary(key => key, _ => AccessLevel.Read, StringComparer.Ordinal);

        var readTooMany = () => cache.GetManyValuesAsync<AccessLevel>(tooManyKeys, "acl");
        var writeTooMany = () => cache.SetManyValuesAsync(tooManyValues, "acl");
        var invalidKey = () => cache.GetManyValuesAsync<AccessLevel>([" "], "acl");

        await readTooMany.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await writeTooMany.Should().ThrowAsync<ArgumentOutOfRangeException>();
        await invalidKey.Should().ThrowAsync<ArgumentException>();

        (await cache.GetManyValuesAsync<AccessLevel>(Array.Empty<string>(), "acl")).Should().BeEmpty();
        await cache.SetManyValuesAsync(new Dictionary<string, AccessLevel>(), "acl");
    }

    private static HybridPermissionCache CreateCache(
        IMemoryCache memory,
        ICacheMetricsService metrics,
        IDistributedCache? distributedCache) =>
        new(
            memory,
            Options.Create(new AuthorizationCacheOptions
            {
                UseDistributedCache = distributedCache is not null,
                AccessControlListTtlSeconds = 60,
                DistributedCacheTtlSeconds = 120,
                MaxL1CacheSize = 1_000
            }),
            metrics,
            NullLogger<HybridPermissionCache>.Instance,
            distributedCache,
            new PermissionCacheKeyTracker(memory, metrics, 1_000));
}
