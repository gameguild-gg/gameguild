using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class UpdateTenantMemberRoleCommandHandlerTests
{
    private readonly Mock<ITenantMemberRepository> _memberRepositoryMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ICacheInvalidationService> _cacheInvalidationServiceMock;
    private readonly Mock<IUserAuthorizationTokenVersionService> _tokenVersionServiceMock;
    private readonly UpdateTenantMemberRoleCommandHandler _handler;

    public UpdateTenantMemberRoleCommandHandlerTests()
    {
        _memberRepositoryMock = new Mock<ITenantMemberRepository>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _cacheInvalidationServiceMock = new Mock<ICacheInvalidationService>();
        _tokenVersionServiceMock = new Mock<IUserAuthorizationTokenVersionService>();
        _tokenVersionServiceMock
            .Setup(service => service.IncrementManyAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handler = new UpdateTenantMemberRoleCommandHandler(
            _memberRepositoryMock.Object,
            _tenantRepositoryMock.Object,
            _cacheInvalidationServiceMock.Object,
            _tokenVersionServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ShouldReturnFailure()
    {
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var result = await _handler.Handle(new UpdateTenantMemberRoleCommand(Guid.NewGuid(), Guid.NewGuid(), "Admin"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Update_Role()
    {
        var member = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Member" };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _memberRepositoryMock.Setup(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _handler.Handle(new UpdateTenantMemberRoleCommand(member.TenantId, member.UserId, "Admin"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.MemberId.Should().Be(member.Id);
        result.NewRole.Should().Be("Admin");
        member.Role.Should().Be("Admin");
        _memberRepositoryMock.Verify(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleUpdateSucceeds_ShouldPersistBeforeInvalidatingUserCache()
    {
        var member = new TenantMember { TenantId = Guid.NewGuid(), UserId = Guid.NewGuid(), Role = "Member" };
        var sequence = new MockSequence();

        _memberRepositoryMock.InSequence(sequence)
            .Setup(r => r.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _memberRepositoryMock.InSequence(sequence)
            .Setup(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _tokenVersionServiceMock.InSequence(sequence)
            .Setup(service => service.IncrementManyAsync(
                It.Is<IReadOnlyCollection<Guid>>(userIds => userIds.SequenceEqual(new[] { member.UserId })),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _cacheInvalidationServiceMock.InSequence(sequence)
            .Setup(s => s.InvalidateUserAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new UpdateTenantMemberRoleCommand(member.TenantId, member.UserId, "Admin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        _cacheInvalidationServiceMock.Verify(
            s => s.InvalidateUserAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()),
            Times.Once);
        _tokenVersionServiceMock.Verify(service => service.IncrementManyAsync(
            It.Is<IReadOnlyCollection<Guid>>(userIds => userIds.SequenceEqual(new[] { member.UserId })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDefaultMembershipIsInactive_Should_ReactivateItWhileUpdatingRole()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var member = new TenantMember
        {
            TenantId = tenant.Id,
            UserId = Guid.NewGuid(),
            Role = "Member",
            IsActive = false,
            LeftAt = DateTime.UtcNow,
            LeaveReason = "Invite cancelled",
            Tenant = tenant
        };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _memberRepositoryMock.Setup(r => r.UpdateAsync(member, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var result = await _handler.Handle(
            new UpdateTenantMemberRoleCommand(member.TenantId, member.UserId, "SystemAdmin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        member.Role.Should().Be("SystemAdmin");
        member.IsActive.Should().BeTrue();
        member.LeftAt.Should().BeNull();
        member.LeaveReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenPromotingSystemAdmin_Should_UpdateTheDefaultMembershipInsteadOfRequestedTenant()
    {
        var userId = Guid.NewGuid();
        var requestedTenant = new Tenant { Id = Guid.NewGuid(), Name = "Studio", Slug = "studio" };
        var defaultTenant = new Tenant { Id = Guid.NewGuid(), Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var requestedMembership = new TenantMember
        {
            TenantId = requestedTenant.Id,
            UserId = userId,
            Role = "Member",
            Tenant = requestedTenant
        };
        var defaultMembership = new TenantMember
        {
            TenantId = defaultTenant.Id,
            UserId = userId,
            Role = "Member",
            Tenant = defaultTenant
        };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, requestedTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestedMembership);
        _memberRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([requestedMembership, defaultMembership]);
        _memberRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember member, CancellationToken _) => member);

        var result = await _handler.Handle(
            new UpdateTenantMemberRoleCommand(requestedTenant.Id, userId, "SystemAdmin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        defaultMembership.Role.Should().Be("SystemAdmin");
        requestedMembership.Role.Should().Be("Member");
        _memberRepositoryMock.Verify(r => r.UpdateAsync(defaultMembership, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPromotingSystemAdmin_ShouldInvalidateTheDefaultAndDemotedMembershipCaches()
    {
        var userId = Guid.NewGuid();
        var staleTenant = new Tenant { Id = Guid.NewGuid(), Name = "Studio", Slug = "studio" };
        var defaultTenant = new Tenant { Id = Guid.NewGuid(), Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var staleMembership = new TenantMember
        {
            TenantId = staleTenant.Id,
            UserId = userId,
            Role = "SystemAdmin",
            Tenant = staleTenant
        };
        var defaultMembership = new TenantMember
        {
            TenantId = defaultTenant.Id,
            UserId = userId,
            Role = "Member",
            Tenant = defaultTenant
        };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, staleTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(staleMembership);
        _memberRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([staleMembership, defaultMembership]);
        _memberRepositoryMock.Setup(r => r.UpdateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember member, CancellationToken _) => member);
        _cacheInvalidationServiceMock
            .Setup(s => s.InvalidateUserAsync(userId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(
            new UpdateTenantMemberRoleCommand(staleTenant.Id, userId, "SystemAdmin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        staleMembership.Role.Should().Be(TenantRole.Admin);
        defaultMembership.Role.Should().Be("SystemAdmin");
        _cacheInvalidationServiceMock.Verify(
            s => s.InvalidateUserAsync(userId, staleTenant.Id, It.IsAny<CancellationToken>()),
            Times.Once);
        _cacheInvalidationServiceMock.Verify(
            s => s.InvalidateUserAsync(userId, defaultTenant.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenPromotingSystemAdminWithoutDefaultMembership_Should_CreateItInDefaultTenant()
    {
        var userId = Guid.NewGuid();
        var requestedTenant = new Tenant { Id = Guid.NewGuid(), Name = "Studio", Slug = "studio" };
        var defaultTenant = new Tenant { Id = Guid.NewGuid(), Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var requestedMembership = new TenantMember
        {
            TenantId = requestedTenant.Id,
            UserId = userId,
            Role = "Member",
            Tenant = requestedTenant
        };
        TenantMember? createdMembership = null;

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, requestedTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(requestedMembership);
        _memberRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([requestedMembership]);
        _tenantRepositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([requestedTenant, defaultTenant]);
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantIncludingDeletedAsync(userId, defaultTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);
        _memberRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()))
            .Callback((TenantMember member, CancellationToken _) => createdMembership = member)
            .ReturnsAsync((TenantMember member, CancellationToken _) => member);

        var result = await _handler.Handle(
            new UpdateTenantMemberRoleCommand(requestedTenant.Id, userId, "SystemAdmin"),
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TenantId.Should().Be(defaultTenant.Id);
        createdMembership.Should().NotBeNull();
        createdMembership!.TenantId.Should().Be(defaultTenant.Id);
        createdMembership.UserId.Should().Be(userId);
        createdMembership.Role.Should().Be("SystemAdmin");
        createdMembership.IsActive.Should().BeTrue();
        requestedMembership.Role.Should().Be("Member");
        _memberRepositoryMock.Verify(
            r => r.CreateAsync(It.Is<TenantMember>(member => member.TenantId == defaultTenant.Id), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDemotingLastSystemAdmin_ShouldRejectUpdate()
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var member = new TenantMember
        {
            TenantId = tenant.Id,
            UserId = Guid.NewGuid(),
            Role = "SystemAdmin",
            Tenant = tenant
        };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(member.UserId, member.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _memberRepositoryMock.Setup(r => r.GetByTenantIdAsync(member.TenantId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([member]);

        var result = await _handler.Handle(new UpdateTenantMemberRoleCommand(member.TenantId, member.UserId, "Member"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("another super admin");
        member.Role.Should().Be("SystemAdmin");
        _memberRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TenantMember>(), It.IsAny<CancellationToken>()), Times.Never);
    }

}
