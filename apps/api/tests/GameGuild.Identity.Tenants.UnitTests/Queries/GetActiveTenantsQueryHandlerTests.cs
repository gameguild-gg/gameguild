using FluentAssertions;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetActiveTenantsQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly GetActiveTenantsQueryHandler _handler;

    public GetActiveTenantsQueryHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new GetActiveTenantsQueryHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithActiveTenants_ShouldReturnActiveTenants()
    {
        // Arrange
        var activeTenants = new List<Tenant>
        {
            new() { Id = Guid.NewGuid(), Name = "Tenant 1", Slug = "tenant-1", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Tenant 2", Slug = "tenant-2", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Tenant 3", Slug = "tenant-3", IsActive = true }
        };

        var query = new GetActiveTenantsQuery();

        _tenantRepositoryMock.Setup(x => x.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTenants);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(t => t.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task Handle_WithNoActiveTenants_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetActiveTenantsQuery();

        _tenantRepositoryMock.Setup(x => x.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        var query = new GetActiveTenantsQuery();

        _tenantRepositoryMock.Setup(x => x.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _tenantRepositoryMock.Verify(x => x.GetActiveTenantsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
