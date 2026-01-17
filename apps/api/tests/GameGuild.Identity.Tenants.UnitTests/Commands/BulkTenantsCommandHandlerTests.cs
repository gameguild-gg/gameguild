using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class BulkTenantsCommandHandlerTests
{
    [Fact]
    public async Task BulkActivate_Should_Count_Success_And_Failures()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var activeTenant = new Tenant { Id = ids[1], Name = "Active", Slug = "active", IsActive = true };
        var inactiveTenant = new Tenant { Id = ids[2], Name = "Inactive", Slug = "inactive", IsActive = false };

        repo.SetupSequence(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null)
            .ReturnsAsync(activeTenant)
            .ReturnsAsync(inactiveTenant);

        var handler = new BulkActivateTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new TestBulkActivateTenantsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(2);
        result.FailedOperations.Should().Be(1);
        repo.Verify(r => r.UpdateAsync(inactiveTenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDeactivate_Should_Count_Success_And_Failures()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var inactiveTenant = new Tenant { Id = ids[1], Name = "Inactive", Slug = "inactive", IsActive = false };
        var activeTenant = new Tenant { Id = ids[2], Name = "Active", Slug = "active", IsActive = true };

        repo.SetupSequence(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null)
            .ReturnsAsync(inactiveTenant)
            .ReturnsAsync(activeTenant);

        var handler = new BulkDeactivateTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new TestBulkDeactivateTenantsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(2);
        result.FailedOperations.Should().Be(1);
        repo.Verify(r => r.UpdateAsync(activeTenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkArchive_Should_Count_Success_And_Failures()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var archivedTenant = new Tenant { Id = ids[1], Name = "Archived", Slug = "archived", IsArchived = true };
        var activeTenant = new Tenant { Id = ids[2], Name = "Active", Slug = "active", IsArchived = false };

        repo.SetupSequence(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null)
            .ReturnsAsync(archivedTenant)
            .ReturnsAsync(activeTenant);

        var handler = new BulkArchiveTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new TestBulkArchiveTenantsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(2);
        result.FailedOperations.Should().Be(1);
        repo.Verify(r => r.UpdateAsync(activeTenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDelete_Should_Soft_Delete_When_Not_HardDelete()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid() };
        var tenant = new Tenant { Id = ids[0], Name = "Tenant", Slug = "tenant" };

        repo.Setup(r => r.GetByIdAsync(ids[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new BulkDeleteTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new TestBulkDeleteTenantsCommand(ids, HardDelete: false), CancellationToken.None);

        result.SuccessfulOperations.Should().Be(1);
        repo.Verify(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkDelete_Should_Hard_Delete_When_Configured()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid() };
        var tenant = new Tenant { Id = ids[0], Name = "Tenant", Slug = "tenant" };

        repo.Setup(r => r.GetByIdAsync(ids[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var handler = new BulkDeleteTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new TestBulkDeleteTenantsCommand(ids, HardDelete: true), CancellationToken.None);

        result.SuccessfulOperations.Should().Be(1);
        repo.Verify(r => r.DeleteAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestBulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkActivateTenantsCommand(TenantIds);

    private sealed record TestBulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkDeactivateTenantsCommand(TenantIds);

    private sealed record TestBulkArchiveTenantsCommand(IEnumerable<Guid> TenantIds) : BulkArchiveTenantsCommand(TenantIds);

    private sealed record TestBulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete) : BulkDeleteTenantsCommand(TenantIds, HardDelete);
}
