using FluentAssertions;
using Moq;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Commands;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new CreateTenantCommandHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateTenant()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Test Tenant",
            "test-tenant",
            "admin@test.com",
            "Test description");

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("test-tenant", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _tenantRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant tenant, CancellationToken _) => tenant);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        _tenantRepositoryMock.Verify(x => x.CreateAsync(
            It.Is<Tenant>(t => 
                t.Name == "Test Tenant" && 
                t.Slug == "test-tenant" &&
                t.AdminEmail == "admin@test.com" &&
                t.IsActive == true), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateSlug_ShouldThrowException()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Test Tenant",
            "existing-slug",
            "admin@test.com",
            null);

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("existing-slug", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(command, CancellationToken.None));
        _tenantRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAllParameters_ShouldCreateTenantWithDescription()
    {
        // Arrange
        var command = new CreateTenantCommand(
            "Full Tenant",
            "full-tenant",
            "admin@full.com",
            "Full description");

        _tenantRepositoryMock.Setup(x => x.IsSlugUniqueAsync("full-tenant", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Tenant? capturedTenant = null;
        _tenantRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .Callback<Tenant, CancellationToken>((tenant, _) => capturedTenant = tenant)
            .ReturnsAsync((Tenant tenant, CancellationToken _) => tenant);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        capturedTenant.Should().NotBeNull();
        capturedTenant!.Name.Should().Be("Full Tenant");
        capturedTenant.Slug.Should().Be("full-tenant");
        capturedTenant.AdminEmail.Should().Be("admin@full.com");
        capturedTenant.Description.Should().Be("Full description");
        capturedTenant.IsActive.Should().BeTrue();
    }
}
