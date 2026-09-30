using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

public sealed class BulkAssignRolesCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _repository = new();
    private readonly Mock<IActorContextAccessor> _actorAccessor = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserAuthorizationTokenVersionService> _tokenVersionService = new();
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationService = new();

    public BulkAssignRolesCommandHandlerTests()
    {
        _tokenVersionService.Setup(service => service.IncrementManyAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheInvalidationService.Setup(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheInvalidationService.Setup(service => service.InvalidateBatchAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<CacheInvalidationTarget>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

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
        _userRepository.Setup(repository => repository.GetByIdsAsync(
                It.Is<IEnumerable<Guid>>(users => users.SequenceEqual(new[] { firstUserId, secondUserId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new User { Id = firstUserId }, new User { Id = secondUserId } });

        var handler = CreateHandler();
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
        _tokenVersionService.Verify(service => service.IncrementManyAsync(
            It.Is<IReadOnlyCollection<Guid>>(userIds => userIds.SequenceEqual(new[] { firstUserId })),
            It.IsAny<CancellationToken>()), Times.Once);
        _cacheInvalidationService.Verify(service => service.InvalidateGlobalAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonSystemAdminIsDeniedBeforeRoleLookup()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("TenantAdmin").Build());
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*system administration*");
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsInvalidBatchBeforeLoadingRole()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = [Guid.NewGuid(), Guid.Empty]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*User IDs cannot be empty*");
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsEmptyRoleIdBeforeRepositoryReads()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.Empty,
            UserIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*A role ID is required*");
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsExpiredAssignmentBeforeRepositoryReads()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = [Guid.NewGuid()],
            ExpiresAt = SystemClock.UtcNow.AddSeconds(-1)
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*expiration must be in the future*");
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveRoleIsRejectedWithoutAssignments()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var roleId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role("Archived", "Inactive", null) { Id = roleId, IsActive = false });
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = roleId,
            UserIds = [Guid.NewGuid()]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Active role*");
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(501)]
    public async Task Handle_RejectsBatchOutsideSupportedBoundsBeforeRepositoryReads(int count)
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = Guid.NewGuid(),
            UserIds = Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Between 1 and 500 users*");
        _repository.Verify(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _userRepository.Verify(repository => repository.GetByIdsAsync(
            It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RejectsUnknownUsersBeforeAssignment()
    {
        SetActor(ActorContextBuilder.ForUser(Guid.NewGuid()).WithRole("SystemAdmin").Build());
        var roleId = Guid.NewGuid();
        var knownUserId = Guid.NewGuid();
        var unknownUserId = Guid.NewGuid();
        _repository.Setup(repository => repository.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role("Moderator", "Moderation", null) { Id = roleId, IsActive = true });
        _userRepository.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new User { Id = knownUserId } });
        var handler = CreateHandler();

        var act = () => handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = roleId,
            UserIds = [knownUserId, unknownUserId]
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*existing, non-deleted users*");
        _repository.Verify(repository => repository.BulkAssignRoleToUsersAsync(
            It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AcceptsMaximumBatchSize()
    {
        var actorId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var userIds = Enumerable.Range(0, 500).Select(_ => Guid.NewGuid()).ToArray();
        SetActor(ActorContextBuilder.ForUser(actorId).WithRole("SystemAdmin").Build());
        _repository.Setup(repository => repository.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role("Moderator", "Moderation", null) { Id = roleId, IsActive = true });
        _userRepository.Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userIds.Select(userId => new User { Id = userId }));
        _repository.Setup(repository => repository.BulkAssignRoleToUsersAsync(
                roleId,
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.SequenceEqual(userIds)),
                actorId,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(userIds.Select(userId => new BulkRoleAssignmentItemResult(
                userId, Guid.NewGuid(), BulkRoleAssignmentStatus.Assigned, SystemClock.UtcNow, null)).ToArray());
        var handler = CreateHandler();

        var result = await handler.Handle(new BulkAssignRolesCommand { RoleId = roleId, UserIds = userIds.ToList() }, CancellationToken.None);

        result.TotalRequested.Should().Be(500);
        result.Assigned.Should().Be(500);
        result.Users.Should().HaveCount(500);
    }

    private void SetActor(ActorContext actor) => _actorAccessor.Setup(accessor => accessor.ActorContext).Returns(actor);

    private BulkAssignRolesCommandHandler CreateHandler()
        => new(_repository.Object, _actorAccessor.Object, _userRepository.Object,
            _tokenVersionService.Object, _cacheInvalidationService.Object);
}
