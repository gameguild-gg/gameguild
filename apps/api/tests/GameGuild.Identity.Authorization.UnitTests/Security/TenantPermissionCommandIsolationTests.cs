using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Security;

public sealed class TenantPermissionCommandIsolationTests
{
    [Fact]
    public async Task GrantTenantPermissionCommand_TenantAdminCannotGrantAcrossTenants()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        var handler = new GrantTenantPermissionCommandHandler(
            service.Object,
            ActorAccessor(actorId, actorTenantId),
            NullLogger<GrantTenantPermissionCommandHandler>.Instance);

        var act = () => handler.Handle(new GrantTenantPermissionCommand
        {
            TenantId = targetTenantId,
            UserId = Guid.NewGuid(),
            Permissions = ["tenant:read"],
            GrantedBy = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GrantTenantPermissionCommand_UsesAuthenticatedActorInsteadOfBodyIdentity()
    {
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var permissionId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        service.Setup(grant => grant.GrantTenantPermissionAsync(
                userId,
                tenantId,
                It.IsAny<string[]>(),
                actorId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TenantPermission { Id = permissionId });
        var handler = new GrantTenantPermissionCommandHandler(
            service.Object,
            ActorAccessor(actorId, tenantId),
            NullLogger<GrantTenantPermissionCommandHandler>.Instance);

        var result = await handler.Handle(new GrantTenantPermissionCommand
        {
            TenantId = tenantId,
            UserId = userId,
            Permissions = ["tenant:read"],
            GrantedBy = Guid.NewGuid()
        }, CancellationToken.None);

        result.Should().Be(permissionId);
        service.VerifyAll();
    }

    [Fact]
    public async Task RevokeTenantPermissionCommand_TenantAdminCannotRevokeAcrossTenants()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        var handler = new RevokeTenantPermissionCommandHandler(
            service.Object,
            ActorAccessor(actorId, actorTenantId),
            NullLogger<RevokeTenantPermissionCommandHandler>.Instance);

        var act = () => handler.Handle(new RevokeTenantPermissionCommand
        {
            TenantId = targetTenantId,
            UserId = Guid.NewGuid(),
            Permissions = ["tenant:read"],
            RevokedBy = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetTenantDefaultPermissionsCommand_TenantAdminCannotChangeAnotherTenant()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        var handler = new SetTenantDefaultPermissionsCommandHandler(
            service.Object,
            ActorAccessor(actorId, actorTenantId),
            NullLogger<SetTenantDefaultPermissionsCommandHandler>.Instance);

        var act = () => handler.Handle(new SetTenantDefaultPermissionsCommand
        {
            TenantId = targetTenantId,
            Permissions = ["tenant:read"],
            SetBy = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DenyTenantPermissionCommand_TenantAdminCannotChangeAnotherTenant()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        var handler = new DenyTenantPermissionCommandHandler(
            service.Object,
            ActorAccessor(actorId, actorTenantId),
            NullLogger<DenyTenantPermissionCommandHandler>.Instance);

        var act = () => handler.Handle(new DenyTenantPermissionCommand
        {
            TenantId = targetTenantId,
            UserId = Guid.NewGuid(),
            Permissions = ["tenant:read"],
            DeniedBy = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        service.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemoveDenyPermissionsCommand_TenantAdminCannotChangeAnotherTenant()
    {
        var actorId = Guid.NewGuid();
        var actorTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var service = new Mock<IPermissionGrantService>();
        var handler = new RemoveDenyPermissionsCommandHandler(
            service.Object,
            ActorAccessor(actorId, actorTenantId),
            NullLogger<RemoveDenyPermissionsCommandHandler>.Instance);

        var act = () => handler.Handle(new RemoveDenyPermissionsCommand
        {
            TenantId = targetTenantId,
            UserId = Guid.NewGuid(),
            Permissions = ["tenant:read"],
            RemovedBy = Guid.NewGuid()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
        service.VerifyNoOtherCalls();
    }

    private static IActorContextAccessor ActorAccessor(Guid actorId, Guid tenantId)
    {
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(context => context.ActorContext)
            .Returns(ActorContextBuilder.ForUser(actorId)
                .WithTenantId(tenantId)
                .WithRole("TenantAdmin")
                .Build());
        return accessor.Object;
    }
}
