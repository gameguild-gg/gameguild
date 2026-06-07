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
        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(tenant, 1);

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

    [Fact]
    public async Task BulkCreate_Should_Count_Success_UniqueConflicts_And_Exceptions()
    {
        var repo = new Mock<ITenantRepository>();
        var items = new[]
        {
            new BulkCreateTenantItem("Created", "created", "created@example.com", "created description"),
            new BulkCreateTenantItem("Duplicate", "duplicate", "duplicate@example.com"),
            new BulkCreateTenantItem("Broken", "broken", "broken@example.com")
        };
        Tenant? createdTenant = null;

        repo.Setup(r => r.IsSlugUniqueAsync("created", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.IsSlugUniqueAsync("duplicate", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repo.Setup(r => r.IsSlugUniqueAsync("broken", It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repo.Setup(r => r.CreateAsync(It.Is<Tenant>(tenant => tenant.Slug == "created"), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((tenant, _) => createdTenant = tenant)
            .ReturnsAsync((Tenant tenant, CancellationToken _) => tenant);
        repo.Setup(r => r.CreateAsync(It.Is<Tenant>(tenant => tenant.Slug == "broken"), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new BulkCreateTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new BulkCreateTenantsCommand(items), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(1);
        result.FailedOperations.Should().Be(2);
        result.Errors.Should().BeEmpty();
        createdTenant.Should().NotBeNull();
        createdTenant!.Name.Should().Be("Created");
        createdTenant.Description.Should().Be("created description");
        createdTenant.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task BulkPurge_Should_Count_Success_MissingTenants_And_Exceptions()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var deletedTenant = new Tenant { Id = ids[1], Name = "Delete Me", Slug = "delete-me" };
        var brokenTenant = new Tenant { Id = ids[2], Name = "Broken", Slug = "broken" };

        repo.Setup(r => r.GetByIdAsync(ids[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        repo.Setup(r => r.GetByIdAsync(ids[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedTenant);
        repo.Setup(r => r.GetByIdAsync(ids[2], It.IsAny<CancellationToken>()))
            .ReturnsAsync(brokenTenant);
        repo.Setup(r => r.DeleteAsync(deletedTenant, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.DeleteAsync(brokenTenant, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new BulkPurgeTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new BulkPurgeTenantsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(1);
        result.FailedOperations.Should().Be(2);
    }

    [Fact]
    public async Task BulkUndelete_Should_Count_Success_MissingTenants_And_Exceptions()
    {
        var repo = new Mock<ITenantRepository>();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var alreadyActiveTenant = new Tenant { Id = ids[1], Name = "Active", Slug = "active" };
        var deletedTenant = new Tenant { Id = ids[2], Name = "Deleted", Slug = "deleted" };
        var brokenTenant = new Tenant { Id = ids[3], Name = "Broken", Slug = "broken" };
        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(deletedTenant, 1);
        typeof(EntityBase).GetProperty(nameof(EntityBase.Version))!.SetValue(brokenTenant, 1);
        deletedTenant.SoftDelete();
        brokenTenant.SoftDelete();

        repo.Setup(r => r.GetByIdAsync(ids[0], It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        repo.Setup(r => r.GetByIdAsync(ids[1], It.IsAny<CancellationToken>()))
            .ReturnsAsync(alreadyActiveTenant);
        repo.Setup(r => r.GetByIdAsync(ids[2], It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedTenant);
        repo.Setup(r => r.GetByIdAsync(ids[3], It.IsAny<CancellationToken>()))
            .ReturnsAsync(brokenTenant);
        repo.Setup(r => r.UpdateAsync(deletedTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedTenant);
        repo.Setup(r => r.UpdateAsync(brokenTenant, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new BulkUndeleteTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new BulkUndeleteTenantsCommand(ids), CancellationToken.None);

        result.TotalRequested.Should().Be(4);
        result.SuccessfulOperations.Should().Be(2);
        result.FailedOperations.Should().Be(2);
        deletedTenant.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task BulkUpdate_Should_Count_Success_MissingTenants_And_Exceptions()
    {
        var repo = new Mock<ITenantRepository>();
        var existingTenant = new Tenant { Id = Guid.NewGuid(), Name = "Old", Slug = "old" };
        var brokenTenant = new Tenant { Id = Guid.NewGuid(), Name = "Broken", Slug = "broken" };
        var missingId = Guid.NewGuid();
        var updates = new[]
        {
            new BulkUpdateTenantItem(missingId, "Missing"),
            new BulkUpdateTenantItem(existingTenant.Id, "Updated", "updated description"),
            new BulkUpdateTenantItem(brokenTenant.Id, "Broken Updated")
        };

        repo.Setup(r => r.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        repo.Setup(r => r.GetByIdAsync(existingTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);
        repo.Setup(r => r.GetByIdAsync(brokenTenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brokenTenant);
        repo.Setup(r => r.UpdateAsync(existingTenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);
        repo.Setup(r => r.UpdateAsync(brokenTenant, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var handler = new BulkUpdateTenantsCommandHandler(repo.Object);
        var result = await handler.Handle(new BulkUpdateTenantsCommand(updates), CancellationToken.None);

        result.TotalRequested.Should().Be(3);
        result.SuccessfulOperations.Should().Be(1);
        result.FailedOperations.Should().Be(2);
        existingTenant.Name.Should().Be("Updated");
        existingTenant.Description.Should().Be("updated description");
    }

    private sealed record TestBulkActivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkActivateTenantsCommand(TenantIds);

    private sealed record TestBulkDeactivateTenantsCommand(IEnumerable<Guid> TenantIds) : BulkDeactivateTenantsCommand(TenantIds);

    private sealed record TestBulkArchiveTenantsCommand(IEnumerable<Guid> TenantIds) : BulkArchiveTenantsCommand(TenantIds);

    private sealed record TestBulkDeleteTenantsCommand(IEnumerable<Guid> TenantIds, bool HardDelete) : BulkDeleteTenantsCommand(TenantIds, HardDelete);
}
