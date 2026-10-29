using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class CachedAclAsyncInvalidationTests
{
    [Theory]
    [InlineData(false, "tenant")]
    [InlineData(false, "global")]
    [InlineData(false, "user")]
    [InlineData(true, "tenant")]
    [InlineData(true, "global")]
    [InlineData(true, "user")]
    public async Task InvalidationDuringL2Read_DiscardsTheOldVersionAllow(bool legacy, string scope)
    {
        using var harness = new CacheHarness();
        var lookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLookup = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var lookupCount = 0;
        harness.Hybrid.Setup(cache => cache.GetValueAsync<CachedAclDecision>(
                It.IsAny<string>(), "acl", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (Interlocked.Increment(ref lookupCount) != 1)
                {
                    return CacheResult<CachedAclDecision>.Miss();
                }

                lookupStarted.TrySetResult();
                await releaseLookup.Task.WaitAsync(TimeSpan.FromSeconds(10));
                return CacheResult<CachedAclDecision>.Hit(new CachedAclDecision(AccessLevel.Write, null));
            });

        var pending = harness.EvaluateAsync(legacy);
        try
        {
            await lookupStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            harness.Revoke(scope);
            releaseLookup.TrySetResult();

            (await pending.WaitAsync(TimeSpan.FromSeconds(10))).Should().Be(AccessLevel.None,
                "a completed invalidation must supersede a lookup still returning the old version's allow");
            harness.SourceReads.Should().Be(1, "the decision must be resolved under the new version");
            (await harness.EvaluateAsync(legacy)).Should().Be(AccessLevel.None);
            harness.SourceReads.Should().Be(1, "the current-version decision remains cacheable");
        }
        finally
        {
            releaseLookup.TrySetResult();
        }
    }

    [Theory]
    [InlineData(false, "tenant")]
    [InlineData(false, "global")]
    [InlineData(false, "user")]
    [InlineData(true, "tenant")]
    [InlineData(true, "global")]
    [InlineData(true, "user")]
    public async Task InvalidationDuringL2Write_DiscardsThePreviouslyResolvedAllow(bool legacy, string scope)
    {
        using var harness = new CacheHarness();
        var writeStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseWrite = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writes = 0;
        harness.Hybrid.Setup(cache => cache.SetValueAsync(
                It.IsAny<string>(), It.IsAny<CachedAclDecision>(), "acl", It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (Interlocked.Increment(ref writes) == 1)
                {
                    writeStarted.TrySetResult();
                    await releaseWrite.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }
            });

        var pending = harness.EvaluateAsync(legacy);
        try
        {
            await writeStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            harness.Revoke(scope);
            releaseWrite.TrySetResult();

            (await pending.WaitAsync(TimeSpan.FromSeconds(10))).Should().Be(AccessLevel.None,
                "an invalidation during a cache write must supersede the previously resolved allow");
            harness.SourceReads.Should().Be(2, "the source must be evaluated again after the invalidation");
            (await harness.EvaluateAsync(legacy)).Should().Be(AccessLevel.None);
            harness.SourceReads.Should().Be(2, "the current-version decision remains cacheable");
        }
        finally
        {
            releaseWrite.TrySetResult();
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StableL2Hit_IsStillReturnedWithoutResolvingTheSource(bool legacy)
    {
        using var harness = new CacheHarness();
        harness.SourceAccess = AccessLevel.None;
        harness.Hybrid.Setup(cache => cache.GetValueAsync<CachedAclDecision>(
                It.IsAny<string>(), "acl", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CacheResult<CachedAclDecision>.Hit(new CachedAclDecision(AccessLevel.Write, null)));

        (await harness.EvaluateAsync(legacy)).Should().Be(AccessLevel.Write);
        (await harness.EvaluateAsync(legacy)).Should().Be(AccessLevel.Write);
        harness.SourceReads.Should().Be(0, "stable L2 results must retain the read-through cache behavior");
        harness.Hybrid.Verify(cache => cache.GetValueAsync<CachedAclDecision>(
            It.IsAny<string>(), "acl", It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed class CacheHarness : IDisposable
    {
        private readonly Guid _tenantId = Guid.NewGuid();
        private readonly Guid _userId = Guid.NewGuid();
        private readonly MemoryCache _memory = new(new MemoryCacheOptions());
        private readonly CachedAccessControlListService _service;
        private long _tenantVersion = 1;
        private long _globalVersion = 1;
        private long _userVersion = 1;

        public Mock<IHybridPermissionCache> Hybrid { get; } = new();
        public AccessLevel SourceAccess { get; set; } = AccessLevel.Write;
        public int SourceReads { get; private set; }

        public CacheHarness()
        {
            var tenantVersions = new Mock<ITenantSecurityVersionStore>();
            tenantVersions.Setup(store => store.GetTenantAndGlobalVersionsAsync(_tenantId, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult((_tenantVersion, _globalVersion)));
            var userVersions = new Mock<IUserSecurityVersionStore>();
            userVersions.Setup(store => store.GetVersionAsync(_userId, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(_userVersion));
            var source = new Mock<IAccessControlListService>();
            source.Setup(service => service.EvaluateAccessAsync(
                    It.IsAny<AclSubject>(), _tenantId, "Resource", "record", It.IsAny<CancellationToken>()))
                .Returns(ReadSourceAsync);
            source.Setup(service => service.GetAccessLevelAsync(
                    _userId, _tenantId, "Resource", "record", It.IsAny<CancellationToken>()))
                .Returns(ReadSourceAsync);

            _service = new CachedAccessControlListService(
                source.Object, _memory, tenantVersions.Object, userVersions.Object,
                Options.Create(new AuthorizationCacheOptions { AccessControlListTtlSeconds = 60 }), Hybrid.Object);
        }

        public Task<AccessLevel> EvaluateAsync(bool legacy) => legacy
            ? _service.GetAccessLevelAsync(_userId, _tenantId, "Resource", "record")
            : _service.EvaluateAccessAsync(AclSubject.ForUser(_userId), _tenantId, "Resource", "record");

        public void Revoke(string scope)
        {
            SourceAccess = AccessLevel.None;
            switch (scope)
            {
                case "tenant": Interlocked.Increment(ref _tenantVersion); break;
                case "global": Interlocked.Increment(ref _globalVersion); break;
                case "user": Interlocked.Increment(ref _userVersion); break;
                default: throw new ArgumentOutOfRangeException(nameof(scope));
            }
        }

        private Task<AccessLevel> ReadSourceAsync()
        {
            SourceReads++;
            return Task.FromResult(SourceAccess);
        }

        public void Dispose() => _memory.Dispose();
    }
}
