using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCachePopularityTrackerTests
{
    [Fact]
    public void Drain_ReturnsMostPopularPairsAndStartsANewWindow()
    {
        var tracker = CreateTracker(capacity: 4);
        var coldRequest = CreateRequest("cold");
        var popularRequest = CreateRequest("popular");

        tracker.Record(coldRequest);
        tracker.Record(popularRequest);
        tracker.Record(popularRequest);
        tracker.Record(popularRequest);

        var candidates = tracker.Drain(minimumAccessCount: 2, maximumEntries: 1);

        candidates.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new PermissionCachePopularityCandidate(popularRequest, AccessCount: 3));
        tracker.Drain(minimumAccessCount: 1, maximumEntries: 4).Should().BeEmpty();
    }

    [Fact]
    public void Record_DoesNotExceedTheConfiguredWindowCapacity()
    {
        var tracker = CreateTracker(capacity: 2);
        var first = CreateRequest("first");
        var second = CreateRequest("second");
        var overflow = CreateRequest("overflow");

        tracker.Record(first);
        tracker.Record(second);
        tracker.Record(overflow);

        tracker.Drain(minimumAccessCount: 1, maximumEntries: 10)
            .Select(candidate => candidate.Request.ResourceId)
            .Should().BeEquivalentTo("first", "second");
    }

    [Fact]
    public void SuppressTracking_IgnoresWarmupLookupsInTheCurrentAsyncFlow()
    {
        var tracker = CreateTracker(capacity: 4);
        var request = CreateRequest("warmup");

        using (tracker.SuppressTracking())
        {
            tracker.Record(request);
        }

        tracker.Drain(minimumAccessCount: 1, maximumEntries: 4).Should().BeEmpty();
        tracker.Record(request);
        tracker.Drain(minimumAccessCount: 1, maximumEntries: 4).Should().ContainSingle();
    }

    [Fact]
    public async Task RunCycleAsync_WarmsPopularEntriesWithoutCountingItsOwnLookups()
    {
        var options = new AuthorizationCacheOptions
        {
            AutomaticWarmupEnabled = true,
            AutomaticWarmupMinimumAccessCount = 2,
            AutomaticWarmupMaxEntriesPerCycle = 10,
            PopularityTrackingCapacity = 10
        };
        var tracker = new PermissionCachePopularityTracker(Options.Create(options));
        var request = CreateRequest("popular");
        tracker.Record(request);
        tracker.Record(request);

        var warmup = new Mock<IPermissionCacheWarmupService>();
        warmup.Setup(service => service.WarmAsync(
                It.IsAny<IReadOnlyCollection<PermissionCacheWarmupRequest>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<PermissionCacheWarmupRequest>, CancellationToken>((requests, _) =>
            {
                tracker.Record(requests.Single());
            })
            .ReturnsAsync(new PermissionCacheWarmupResult(1, 1, 0));
        var services = new ServiceCollection();
        services.AddScoped<IPermissionCacheWarmupService>(_ => warmup.Object);
        await using var provider = services.BuildServiceProvider();
        var service = new AutomaticPermissionCacheWarmupService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            tracker,
            Options.Create(options),
            NullLogger<AutomaticPermissionCacheWarmupService>.Instance);

        var result = await service.RunCycleAsync();

        result.Should().Be(new PermissionCacheWarmupResult(1, 1, 0));
        warmup.Verify(warmupService => warmupService.WarmAsync(
            It.Is<IReadOnlyCollection<PermissionCacheWarmupRequest>>(requests => requests.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
        tracker.Drain(minimumAccessCount: 1, maximumEntries: 10).Should().BeEmpty();
    }

    [Fact]
    public void DisabledTracking_DoesNotRetainRequests()
    {
        var tracker = new PermissionCachePopularityTracker(Options.Create(new AuthorizationCacheOptions
        {
            AutomaticWarmupEnabled = false,
            PopularityTrackingCapacity = 2
        }));

        tracker.Record(CreateRequest("ignored"));

        tracker.Drain(minimumAccessCount: 1, maximumEntries: 2).Should().BeEmpty();
    }

    [Fact]
    public async Task CachedAclService_TracksRepeatedRequestsIncludingCacheHits()
    {
        var options = new AuthorizationCacheOptions
        {
            AutomaticWarmupEnabled = true,
            PopularityTrackingCapacity = 10
        };
        var tracker = new PermissionCachePopularityTracker(Options.Create(options));
        var tenantId = Guid.NewGuid();
        var request = CreateRequest("tracked");
        request = request with { TenantId = tenantId };
        var innerService = new Mock<IAccessControlListService>();
        innerService.Setup(service => service.EvaluateAccessAsync(
                request.Subject,
                tenantId,
                request.ResourceType,
                request.ResourceId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccessLevel.Read);
        var tenantVersionStore = new Mock<ITenantSecurityVersionStore>();
        tenantVersionStore.Setup(store => store.GetTenantAndGlobalVersionsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((0L, 0L));
        var userVersionStore = new Mock<IUserSecurityVersionStore>();
        userVersionStore.Setup(store => store.GetVersionAsync(request.Subject.UserId!.Value, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0L);
        var service = new CachedAccessControlListService(
            innerService.Object,
            new MemoryCache(new MemoryCacheOptions()),
            tenantVersionStore.Object,
            userVersionStore.Object,
            Options.Create(options),
            hybridCache: null,
            metrics: null,
            keyTracker: null,
            invalidationService: null,
            popularityTracker: tracker);

        for (var i = 0; i < 3; i++)
        {
            await service.EvaluateAccessAsync(
                request.Subject,
                tenantId,
                request.ResourceType,
                request.ResourceId);
        }

        tracker.Drain(minimumAccessCount: 3, maximumEntries: 10)
            .Should().ContainSingle()
            .Which.Request.Should().BeEquivalentTo(request);
        innerService.Verify(service => service.EvaluateAccessAsync(
            It.IsAny<AclSubject>(),
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void AuthorizationApplication_RegistersAutomaticWarmupWorkerWithCachingEnabled()
    {
        var services = new ServiceCollection();

        services.AddAuthorizationApplication(enableCaching: true);

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(IHostedService) &&
            descriptor.ImplementationType == typeof(AutomaticPermissionCacheWarmupService));
    }

    [Fact]
    public void AuthorizationCacheOptions_RejectInvalidAutomaticWarmupLimits()
    {
        var invalidInterval = new AuthorizationCacheOptions { AutomaticWarmupIntervalSeconds = 0 };
        var invalidBatchSize = new AuthorizationCacheOptions { AutomaticWarmupMaxEntriesPerCycle = 501 };

        var validateInterval = () => invalidInterval.Validate();
        var validateBatchSize = () => invalidBatchSize.Validate();

        validateInterval.Should().Throw<InvalidOperationException>()
            .WithMessage("AutomaticWarmupIntervalSeconds must be positive.");
        validateBatchSize.Should().Throw<InvalidOperationException>()
            .WithMessage("AutomaticWarmupMaxEntriesPerCycle must be between 1 and 500.");
    }

    private static PermissionCachePopularityTracker CreateTracker(int capacity)
    {
        return new PermissionCachePopularityTracker(Options.Create(new AuthorizationCacheOptions
        {
            AutomaticWarmupEnabled = true,
            PopularityTrackingCapacity = capacity
        }));
    }

    private static PermissionCacheWarmupRequest CreateRequest(string resourceId)
    {
        return new PermissionCacheWarmupRequest(
            Guid.NewGuid(),
            AclSubject.ForUser(Guid.NewGuid()),
            "Document",
            resourceId);
    }
}
