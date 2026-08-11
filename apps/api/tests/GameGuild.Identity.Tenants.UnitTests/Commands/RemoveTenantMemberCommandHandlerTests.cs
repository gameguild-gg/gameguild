using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class RemoveTenantMemberCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ITenantMemberRepository> _memberRepositoryMock;
    private readonly RemoveTenantMemberCommandHandler _handler;

    public RemoveTenantMemberCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _memberRepositoryMock = new Mock<ITenantMemberRepository>();
        _handler = new RemoveTenantMemberCommandHandler(_tenantRepositoryMock.Object, _memberRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenMemberNotFound_ShouldReturnFailure()
    {
        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantMember?)null);

        var result = await _handler.Handle(new TestRemoveTenantMemberCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_Should_Delete_Member_And_Add_Event_When_Tenant_Found()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member" };
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant" };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new TestRemoveTenantMemberCommand(tenantId, userId), CancellationToken.None);

        result.Success.Should().BeTrue();
        _memberRepositoryMock.Verify(r => r.DeleteAsync(member.Id, It.IsAny<CancellationToken>()), Times.Once);
        tenant.DomainEvents.Should().Contain(e => e is TenantMemberRemovedEvent);
    }

    [Fact]
    public async Task Handle_Should_Delete_Member_Without_Event_When_Tenant_Not_Found()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var member = new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member" };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new TestRemoveTenantMemberCommand(tenantId, userId), CancellationToken.None);

        result.Success.Should().BeFalse();
        _memberRepositoryMock.Verify(r => r.DeleteAsync(member.Id, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTenantIsDefault_Should_RejectRemoval()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "GameGuild", Slug = "gameguild", IsDefault = true };
        var member = new TenantMember { TenantId = tenantId, UserId = userId, Role = "Member", Tenant = tenant };

        _memberRepositoryMock.Setup(r => r.GetByUserAndTenantAsync(userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new TestRemoveTenantMemberCommand(tenantId, userId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("default tenant");
        _memberRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed record TestRemoveTenantMemberCommand(Guid TenantId, Guid UserId)
        : RemoveTenantMemberCommand(TenantId, UserId);
}
