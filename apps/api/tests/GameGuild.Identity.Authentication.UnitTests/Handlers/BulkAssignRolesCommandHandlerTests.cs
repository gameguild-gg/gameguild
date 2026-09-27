using FluentAssertions;
using GameGuild.Identity.Context.Actors;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class BulkAssignRolesCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();

    [Fact]
    public async Task Handle_SystemAdminAssignsOnceAndReturnsPerUserOutcomes()
    {
        var actorId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();
        var expiresAt = SystemClock.UtcNow.AddDays(30);
        SetActor(ActorContextBuilder.ForUser(actorId).WithRole("SystemAdmin").Build());
        var role = new Role("Moderator", "Moderation", null) { Id = roleId, IsActive = true };
        _repository.Setup(repository => repository.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _repository.Setup(repository => repository.BulkAssignRoleToUsersAsync(
                roleId,
                It.Is<IReadOnlyCollection<Guid>>(users => users.SequenceEqual(new[] { firstUserId, secondUserId })),
                actorId,
                expiresAt,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BulkRoleAssignmentItemResult>
            {
                new(firstUserId, Guid.NewGuid(), BulkRoleAssignmentStatus.Assigned, SystemClock.UtcNow, expiresAt),
                new(secondUserId, Guid.NewGuid(), BulkRoleAssignmentStatus.AlreadyAssigned, SystemClock.UtcNow, expiresAt)
            });

        var handler = new BulkAssignRolesCommandHandler(_repository.Object, _actorAccessor.Object);
        var result = await handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = roleId,
            UserIds = [firstUserId, secondUserId, firstUserId],
            ExpiresAt = expiresAt
        }, CancellationToken.None);

        result.RoleId.Should().Be(roleId);
        result.TotalRequested.Should().Be(2);
        result.DuplicateUserIds.Should().Be(1);
        result.Assigned.Should().Be(1);
        result.Reactivated.Should().Be(0);
        result.AlreadyAssigned.Should().Be(1);
        result.Users.Should().HaveCount(2);
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            roleId,
            It.IsAny<IReadOnlyCollection<Guid>>(),
            actorId,
            expiresAt,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonSystemAdminIsDeniedBeforeRoleLookup()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("TenantAdmin").Build());
        var handler = new BulkAssignRolesCommandHandler(_repository.Object, _actorAccessor.Object);

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*system administration*");
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsInvalidBatchBeforeLoadingRole()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = new BulkAssignRolesCommandHandler(_repository.Object, _actorAccessor.Object);

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = [Guid.NewGuid(), Guid.Empty]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*User IDs cannot be empty*");
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveRoleIsRejectedWithoutAssignments()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var roleId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role("Archived", "Inactive", null) { Id = roleId, IsActive = false });
        var handler = new BulkAssignRolesCommandHandler(_repository.Object, _actorAccessor.Object);

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = roleId,
            UserIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Active role*");
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetActor(ActorContext actor) => _actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);
}
