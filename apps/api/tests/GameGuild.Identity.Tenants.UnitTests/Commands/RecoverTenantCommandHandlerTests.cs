using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class RecoverTenantCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithArchivedTenant_ShouldUnarchiveAndPersistTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Archived tenant",
            Slug = "archived-tenant",
            IsActive = true,
        };
        tenant.Archive("test fixture");

        var repository = new Mock<ITenantRepository>();
        repository.Setup(candidate => candidate.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        repository.Setup(candidate => candidate.UpdateAsync(tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new RecoverTenantCommandHandler(repository.Object);

        var result = await handler.Handle(new RecoverTenantCommand(tenantId, "restore fixture"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.TenantId.Should().Be(tenantId);
        tenant.IsArchived.Should().BeFalse();
        tenant.IsActive.Should().BeTrue();
        repository.Verify(candidate => candidate.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMissingTenant_ShouldReturnFailureWithoutPersisting()
    {
        var tenantId = Guid.NewGuid();
        var repository = new Mock<ITenantRepository>();
        repository.Setup(candidate => candidate.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        var handler = new RecoverTenantCommandHandler(repository.Object);

        var result = await handler.Handle(new RecoverTenantCommand(tenantId, "restore fixture"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.TenantId.Should().Be(tenantId);
        result.Message.Should().Contain("not found");
        repository.Verify(candidate => candidate.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithActiveTenant_ShouldSucceedWithoutPersisting()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Active tenant",
            Slug = "active-tenant",
            IsActive = true,
        };
        var repository = new Mock<ITenantRepository>();
        repository.Setup(candidate => candidate.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new RecoverTenantCommandHandler(repository.Object);

        var result = await handler.Handle(new RecoverTenantCommand(tenantId, "restore fixture"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Be("Tenant is not archived");
        repository.Verify(candidate => candidate.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}