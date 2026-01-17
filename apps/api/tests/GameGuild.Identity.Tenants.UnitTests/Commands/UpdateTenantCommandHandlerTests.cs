using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class UpdateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly UpdateTenantCommandHandler _handler;

    public UpdateTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new UpdateTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithNullRequest_ShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldThrow()
    {
        var tenantId = Guid.NewGuid();
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var act = () => _handler.Handle(new UpdateTenantCommand(tenantId, "Name", "Desc"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Handle_Should_Update_Tenant_Name_And_Description()
    {
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, Name = "Old", Slug = "old" };

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _tenantRepositoryMock.Setup(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        var result = await _handler.Handle(new UpdateTenantCommand(tenantId, "New", "Updated"), CancellationToken.None);

        result.Should().Be(GameGuild.CQRS.Unit.Value);
        tenant.Name.Should().Be("New");
        tenant.Description.Should().Be("Updated");
    }
}
