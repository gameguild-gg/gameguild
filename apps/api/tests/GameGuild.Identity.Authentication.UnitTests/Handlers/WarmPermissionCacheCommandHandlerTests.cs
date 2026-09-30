using FluentAssertions;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class WarmPermissionCacheCommandHandlerTests
{
    [Fact]
    public async Task Handle_SystemAdminWarmsRequestedUserResources()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var result = new PermissionCacheWarmupResult(1, 1, 0);
        var warmupService = new Mock<IPermissionCacheWarmupService>();
        warmupService.Setup(service => service.WarmAsync(
                It.Is<IReadOnlyCollection<PermissionCacheWarmupRequest>>(items =>
                    items.Count == 1 && items.Single().TenantId == tenantId &&
                    items.Single().Subject.UserId == userId && items.Single().ResourceId == "doc-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(
            ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = new WarmPermissionCacheCommandHandler(actorAccessor.Object, warmupService.Object);

        var actual = await handler.Handle(new WarmPermissionCacheCommand
        {
            TenantId = tenantId,
            Items = [new WarmPermissionCacheItem { UserId = userId, ResourceType = "Document", ResourceId = "doc-1" }]
        }, CancellationToken.None);

        actual.Should().Be(result);
        warmupService.Verify(service => service.WarmAsync(It.IsAny<IReadOnlyCollection<PermissionCacheWarmupRequest>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RejectsNonAdminBeforeCallingWarmupService()
    {
        var warmupService = new Mock<IPermissionCacheWarmupService>();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.Setup(accessor => accessor.ActorContext).Returns(ActorContextBuilder.ForUser(Guid.NewGuid()).Build());
        var handler = new WarmPermissionCacheCommandHandler(actorAccessor.Object, warmupService.Object);

        var act = () => handler.Handle(new WarmPermissionCacheCommand { TenantId = Guid.NewGuid() }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        warmupService.Verify(service => service.WarmAsync(It.IsAny<IReadOnlyCollection<PermissionCacheWarmupRequest>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
