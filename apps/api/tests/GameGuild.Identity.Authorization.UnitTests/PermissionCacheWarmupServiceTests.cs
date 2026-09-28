using FluentAssertions;
using GameGuild.Identity.Authorization.Caching;
using Moq;

namespace GameGuild.Identity.Authorization.UnitTests;

public sealed class PermissionCacheWarmupServiceTests
{
    [Fact]
    public async Task WarmAsync_EvaluatesEachDistinctSubjectResourcePairOnce()
    {
        var accessControlList = new Mock<IAccessControlListService>();
        accessControlList.Setup(service => service.EvaluateAccessAsync(
                It.IsAny<AclSubject>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AccessLevel.Read);
        var service = new PermissionCacheWarmupService(accessControlList.Object);
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var requests = new[]
        {
            new PermissionCacheWarmupRequest(
                tenantId, AclSubject.ForUser(userId, roleIds), "Document", "doc-1"),
            new PermissionCacheWarmupRequest(
                tenantId, AclSubject.ForUser(userId, roleIds.Reverse().ToArray()), "Document", "doc-1"),
            new PermissionCacheWarmupRequest(
                tenantId, AclSubject.ForUser(userId, roleIds), "Document", "doc-2")
        };

        var result = await service.WarmAsync(requests);

        result.Should().Be(new PermissionCacheWarmupResult(Requested: 3, Warmed: 2, DuplicatesSkipped: 1));
        accessControlList.Verify(service => service.EvaluateAccessAsync(
            It.IsAny<AclSubject>(), tenantId, "Document", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task WarmAsync_RejectsInvalidTenantAndOversizedBatches()
    {
        var accessControlList = new Mock<IAccessControlListService>();
        var service = new PermissionCacheWarmupService(accessControlList.Object);
        var invalidTenant = new PermissionCacheWarmupRequest(
            Guid.Empty, AclSubject.ForUser(Guid.NewGuid()), "Document", "doc-1");

        var invalidTenantAct = () => service.WarmAsync([invalidTenant]);
        var oversizedAct = () => service.WarmAsync(Enumerable.Repeat(invalidTenant, 501).ToArray());

        await invalidTenantAct.Should().ThrowAsync<ArgumentException>();
        await oversizedAct.Should().ThrowAsync<ArgumentOutOfRangeException>();
        accessControlList.Verify(service => service.EvaluateAccessAsync(
            It.IsAny<AclSubject>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
