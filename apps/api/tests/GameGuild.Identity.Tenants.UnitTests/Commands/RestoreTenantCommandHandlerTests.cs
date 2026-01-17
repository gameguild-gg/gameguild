using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class RestoreTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly RestoreTenantCommandHandler _handler;

    public RestoreTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new RestoreTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnFailure()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var result = await _handler.Handle(new TestRestoreTenantCommand(tenantId), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenTenantNotArchived_ShouldReturnSuccess()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = false };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new TestRestoreTenantCommand(tenantId), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("not archived");
        _tenantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRestoreTenant()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Tenant", Slug = "tenant", IsArchived = true, IsActive = false };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _tenantRepositoryMock.Setup(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new TestRestoreTenantCommand(tenantId), CancellationToken.None);

        result.Success.Should().BeTrue();
        tenant.IsArchived.Should().BeFalse();
        tenant.IsActive.Should().BeTrue();
        _tenantRepositoryMock.Verify(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
    }

    private sealed record TestRestoreTenantCommand(Guid TenantId) : RestoreTenantCommand(TenantId);
}
