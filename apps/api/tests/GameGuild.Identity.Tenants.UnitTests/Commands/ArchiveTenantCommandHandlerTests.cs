using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class ArchiveTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly ArchiveTenantCommandHandler _handler;

    public ArchiveTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new ArchiveTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new ArchiveTenantCommand(tenantId, "reason"), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenTenantAlreadyArchived_ShouldReturnSuccess()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = true };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new ArchiveTenantCommand(tenantId, "reason"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("already archived");
        _tenantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldArchiveTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsActive = true };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _tenantRepositoryMock.Setup(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new ArchiveTenantCommand(tenantId, "reason"), CancellationToken.None);

        result.Success.Should().BeTrue();
        tenant.IsArchived.Should().BeTrue();
        tenant.IsActive.Should().BeFalse();
        _tenantRepositoryMock.Verify(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }
}
