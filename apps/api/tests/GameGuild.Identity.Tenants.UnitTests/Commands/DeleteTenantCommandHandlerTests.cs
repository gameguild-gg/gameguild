using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class DeleteTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<ITenantAdministrationGuard> _administrationGuardMock;
    private readonly DeleteTenantCommandHandler _handler;

    public DeleteTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _administrationGuardMock = new Mock<ITenantAdministrationGuard>();
        _administrationGuardMock
            .Setup(guard => guard.EnsureCanPurgeAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _handler = new DeleteTenantCommandHandler(_tenantRepositoryMock.Object, _administrationGuardMock.Object);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Should_ConsultThePurgeGuard()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = true });

        await _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        _administrationGuardMock.Verify(guard => guard.EnsureCanPurgeAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once,
            "issue #1287 T1: hard deletion is the destructive tier and must be gated");
    }

    [Fact]
    public async Task Handle_WhenTenantIsMissing_ShouldThrowEntityNotFound()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var act = () => _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        await act.Should().ThrowAsync<EntityNotFoundException>();
        _tenantRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _tenantRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTenantIsNotArchived_ShouldEnforceTheDeletionContract()
    {
        // Issue #1287 T1a: ValidateForDeletion requires the tenant to be archived and
        // memberless first — the delete handler consults the entity contract now.
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = false };
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var act = () => _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .Where(ex => ex.Message.Contains("archived", StringComparison.OrdinalIgnoreCase));
        _tenantRepositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTenantHasActiveMembers_ShouldEnforceTheDeletionContract()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = true };
        tenant.TenantMembers.Add(new TenantMember { TenantId = tenantId, UserId = Guid.NewGuid(), Role = "Member", IsActive = true });
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var act = () => _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .Where(ex => ex.Message.Contains("active members", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Handle_WhenTheDeletionContractIsSatisfied_ShouldDeleteTheTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = true };
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new DeleteTenantCommand(tenantId), CancellationToken.None);

        result.Should().Be(GameGuild.CQRS.Unit.Value);
        _tenantRepositoryMock.Verify(r => r.DeleteAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }
}
