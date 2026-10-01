using FluentAssertions;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class CachedAccessControlListCacheCapacityTests
{
    [Fact]
    public async Task EvaluateAccessAsync_SetsCacheEntrySizeWhenMemoryCacheHasSizeLimit()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var innerService = new Mock<IAccessControlListService>(MockBehavior.Strict);
        innerService
            .Setup(service => service.EvaluateAccessAsync(
                It.IsAny<AclSubject>(),
                tenantId,
                "Document",
                "doc-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccessLevel.Write);

        var tenantVersions = new Mock<ITenantSecurityVersionStore>(MockBehavior.Strict);
        tenantVersions
            .Setup(store => store.GetTenantAndGlobalVersionsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1L, 1L));

        var userVersions = new Mock<IUserSecurityVersionStore>(MockBehavior.Strict);
        userVersions
            .Setup(store => store.GetVersionAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);

        using var memoryCache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 10 });
        var service = new CachedAccessControlListService(
            innerService.Object,
            memoryCache,
            tenantVersions.Object,
            userVersions.Object,
            Options.Create(new AuthorizationCacheOptions { AccessControlListTtlSeconds = 60 }));

        var result = await service.EvaluateAccessAsync(
            AclSubject.ForUser(userId),
            tenantId,
            "Document",
            "doc-1");

        result.Should().Be(AccessLevel.Write);
        innerService.Verify(service => service.EvaluateAccessAsync(
            It.IsAny<AclSubject>(), tenantId, "Document", "doc-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
