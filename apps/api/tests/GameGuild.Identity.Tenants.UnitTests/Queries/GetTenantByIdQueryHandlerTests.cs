using FluentAssertions;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Tenants;
using GameGuild.Identity.Tenants;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Queries;

public class GetTenantByIdQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly GetTenantByIdQueryHandler _handler;

    public GetTenantByIdQueryHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new GetTenantByIdQueryHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithExistingTenant_ShouldReturnTenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Test Tenant",
            Slug = "test-tenant",
            AdminEmail = "admin@test.com",
            IsActive = true
        };

        var query = new GetTenantByIdQuery(tenantId);

        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(tenantId);
        result.Name.Should().Be("Test Tenant");
        result.Slug.Should().Be("test-tenant");
        result.AdminEmail.Should().Be("admin@test.com");
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNonExistingTenant_ShouldReturnNull()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetTenantByIdQuery(tenantId);

        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldCallRepositoryOnce()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var query = new GetTenantByIdQuery(tenantId);

        _tenantRepositoryMock.Setup(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _tenantRepositoryMock.Verify(x => x.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
