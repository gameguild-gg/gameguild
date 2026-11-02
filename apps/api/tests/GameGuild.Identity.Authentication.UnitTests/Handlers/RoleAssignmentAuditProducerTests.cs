using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using Moq;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Handlers;

/// <summary>
///     Pins the role-lifecycle audit producers (issue #1204): assign, bulk assign and
///     remove emit <c>RoleAssigned</c>/<c>RoleRevoked</c> through
///     <see cref="IRoleAssignmentAuditEventSink"/> — the action types the compliance
///     event-coverage checks list but that previously had no producer.
/// </summary>
public sealed class RoleAssignmentAuditProducerTests
{
    private readonly Mock<IRoleRepository> _roleRepository = new();
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IUserAuthorizationTokenVersionService> _tokenVersionService = new();
    private readonly Mock<ICacheInvalidationService> _cacheInvalidation = new();
    private readonly Mock<IRoleAssignmentAuditEventSink> _auditSink = new();

    private static Role CreateRole(Guid tenantId) => new("Support", "Support role", tenantId)
    {
        Permissions = "[\"courses:read\"]"
    };

    private static IActorContextAccessor CreateSystemAdminActor(Guid userId, Guid tenantId)
    {
        var actor = new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = tenantId,
            Roles = new HashSet<string>(["SystemAdmin"], StringComparer.Ordinal),
            Permissions = new HashSet<string>(StringComparer.Ordinal),
            IsAuthenticated = true
        };
        var accessor = new Mock<IActorContextAccessor>();
        accessor.SetupGet(access => access.ActorContext).Returns(actor);
        return accessor.Object;
    }

    [Fact]
    public async Task AssignRole_EmitsRoleAssignedAuditEvent()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = CreateRole(tenantId);
        _roleRepository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepository.Setup(r => r.UserHasRoleAsync(userId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _roleRepository.Setup(r => r.AssignRoleToUserAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole userRole, CancellationToken _) => userRole);
        var handler = new AssignRoleToUserCommandHandler(
            _roleRepository.Object, _cacheInvalidation.Object, _auditSink.Object);

        await handler.Handle(new AssignRoleToUserCommand
        {
            UserId = userId,
            RoleId = role.Id,
            AssignedBy = adminId,
            ExpiresAt = null
        }, CancellationToken.None);

        _auditSink.Verify(
            sink => sink.RecordAsync(
                It.Is<RoleAssignmentAuditEvent>(audit =>
                    audit.ActionType == RoleAssignmentAuditActionTypes.RoleAssigned
                    && audit.SubjectUserId == userId
                    && audit.RoleId == role.Id
                    && audit.RoleName == "Support"
                    && audit.TenantId == tenantId
                    && audit.PerformedBy == adminId),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "role assignment must emit the RoleAssigned audit action the coverage checks expect");
    }

    [Fact]
    public async Task AssignRole_DuplicateAssignment_EmitsNothing()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = CreateRole(tenantId);
        _roleRepository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _roleRepository.Setup(r => r.UserHasRoleAsync(userId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new AssignRoleToUserCommandHandler(
            _roleRepository.Object, _cacheInvalidation.Object, _auditSink.Object);

        var act = () => handler.Handle(new AssignRoleToUserCommand { UserId = userId, RoleId = role.Id },
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _auditSink.Verify(
            sink => sink.RecordAsync(It.IsAny<RoleAssignmentAuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "a rejected assignment is not an audit event");
    }

    [Fact]
    public async Task RemoveRole_EmitsRoleRevokedAuditEvent()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var role = CreateRole(tenantId);
        _roleRepository.Setup(r => r.UserHasRoleAsync(userId, role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _roleRepository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        var handler = new RemoveRoleFromUserCommandHandler(
            _roleRepository.Object, _cacheInvalidation.Object, _auditSink.Object);

        await handler.Handle(new RemoveRoleFromUserCommand { UserId = userId, RoleId = role.Id },
            CancellationToken.None);

        _auditSink.Verify(
            sink => sink.RecordAsync(
                It.Is<RoleAssignmentAuditEvent>(audit =>
                    audit.ActionType == RoleAssignmentAuditActionTypes.RoleRevoked
                    && audit.SubjectUserId == userId
                    && audit.RoleId == role.Id
                    && audit.RoleName == "Support"
                    && audit.TenantId == tenantId),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "role removal must emit the RoleRevoked audit action the coverage checks expect");
    }

    [Fact]
    public async Task BulkAssign_EmitsOneRoleAssignedEventPerChangedUser()
    {
        var tenantId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var userC = Guid.NewGuid();
        var role = CreateRole(tenantId);
        _roleRepository.Setup(r => r.GetByIdAsync(role.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);
        _userRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new User { Id = userA }, new User { Id = userB }, new User { Id = userC }]);
        _roleRepository
            .Setup(r => r.BulkAssignRoleToUsersAsync(
                role.Id, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid?>(),
                It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                new BulkRoleAssignmentItemResult(userA, Guid.NewGuid(), BulkRoleAssignmentStatus.Assigned, SystemClock.UtcNow, null),
                new BulkRoleAssignmentItemResult(userB, Guid.NewGuid(), BulkRoleAssignmentStatus.Reactivated, SystemClock.UtcNow, null),
                new BulkRoleAssignmentItemResult(userC, Guid.NewGuid(), BulkRoleAssignmentStatus.AlreadyAssigned, SystemClock.UtcNow, null)
            ]);
        _tokenVersionService
            .Setup(r => r.IncrementManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var handler = new BulkAssignRolesCommandHandler(
            _roleRepository.Object,
            CreateSystemAdminActor(adminId, tenantId),
            _userRepository.Object,
            _tokenVersionService.Object,
            _cacheInvalidation.Object,
            _auditSink.Object);

        await handler.Handle(new BulkAssignRolesCommand
        {
            RoleId = role.Id,
            UserIds = [userA, userB, userC]
        }, CancellationToken.None);

        _auditSink.Verify(
            sink => sink.RecordAsync(
                It.Is<RoleAssignmentAuditEvent>(audit =>
                    audit.ActionType == RoleAssignmentAuditActionTypes.RoleAssigned
                    && audit.PerformedBy == adminId
                    && (audit.SubjectUserId == userA || audit.SubjectUserId == userB)),
                It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "only changed assignments (Assigned/Reactivated) are audited");
        _auditSink.Verify(
            sink => sink.RecordAsync(
                It.Is<RoleAssignmentAuditEvent>(audit => audit.SubjectUserId == userC),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "an unchanged assignment is not an audit event");
    }
}
