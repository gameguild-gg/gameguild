using FluentAssertions;
using GameGuild.Core.Exceptions;
using GameGuild.Modules.Tenants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Services;

/// <summary>
/// Unit tests for TenantService
/// </summary>
public class TenantServiceTests
{
    private readonly Mock<ITenantRepository> _mockRepository;
    private readonly Mock<ITenantSettingsService> _mockTenantSettingsService;
    private readonly Mock<ITenantDomainsService> _mockTenantDomainsService;
    private readonly Mock<ITenantCacheService> _mockCacheService;
    private readonly Mock<ILogger<TenantService>> _mockLogger;
    private readonly TenantService _tenantService;

    public TenantServiceTests()
    {
        _mockRepository = new Mock<ITenantRepository>();
        _mockTenantSettingsService = new Mock<ITenantSettingsService>();
        _mockTenantDomainsService = new Mock<ITenantDomainsService>();
        _mockCacheService = new Mock<ITenantCacheService>();
        _mockLogger = new Mock<ILogger<TenantService>>();

        _tenantService = new TenantService(
            _mockRepository.Object,
            _mockTenantSettingsService.Object,
            _mockTenantDomainsService.Object,
            _mockCacheService.Object,
            _mockLogger.Object
        );
    }

    #region GetActiveTenantsAsync Tests

    [Fact]
    public async Task GetActiveTenantsAsync_Should_Return_Active_Tenants()
    {
        // Arrange
        var expectedTenants = new List<Tenant>
        {
            new() { Id = Guid.NewGuid(), Name = "Tenant 1", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Tenant 2", IsActive = true }
        };

        _ = _mockRepository.Setup(r => r.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenants);

        // Act
        IReadOnlyList<Tenant> result = await _tenantService.GetActiveTenantsAsync();

        // Assert
        _ = result.Should().BeEquivalentTo(expectedTenants);
        _mockRepository.Verify(r => r.GetActiveTenantsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetActiveTenantsAsync_Should_Handle_Empty_List()
    {
        // Arrange
        var emptyList = new List<Tenant>();
        _ = _mockRepository.Setup(r => r.GetActiveTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        IReadOnlyList<Tenant> result = await _tenantService.GetActiveTenantsAsync();

        // Assert
        _ = result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetActiveTenantsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetTenantByIdAsync Tests

    [Fact]
    public async Task GetTenantByIdAsync_Should_Return_Cached_Tenant_When_Available()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var cachedTenant = new Tenant { Id = tenantId, Name = "Cached Tenant" };

        _ = _mockCacheService.Setup(c => c.GetTenantById(tenantId))
            .Returns(cachedTenant);

        // Act
        Tenant? result = await _tenantService.GetTenantByIdAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(cachedTenant);
        _mockCacheService.Verify(c => c.GetTenantById(tenantId), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetTenantByIdAsync_Should_Fallback_To_Database_When_Not_Cached()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var dbTenant = new Tenant { Id = tenantId, Name = "DB Tenant" };

        _ = _mockCacheService.Setup(c => c.GetTenantById(tenantId))
            .Returns((Tenant?)null);

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbTenant);

        // Act
        Tenant? result = await _tenantService.GetTenantByIdAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(dbTenant);
        _mockCacheService.Verify(c => c.GetTenantById(tenantId), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenantByIdAsync_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _ = _mockCacheService.Setup(c => c.GetTenantById(tenantId))
            .Returns((Tenant?)null);

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        Tenant? result = await _tenantService.GetTenantByIdAsync(tenantId);

        // Assert
        _ = result.Should().BeNull();
        _mockCacheService.Verify(c => c.GetTenantById(tenantId), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateTenantAsync Tests

    [Fact]
    public async Task CreateTenantAsync_Should_Create_Tenant_Successfully()
    {
        // Arrange
        const string name = "New Tenant";
        const string slug = "new-tenant";
        const string description = "A new tenant";

        var expectedTenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Description = description,
            IsActive = true,
            IsDefault = false
        };

        // Setup repository to return true for slug availability check
        _ = _mockRepository.Setup(r => r.IsSlugAvailableAsync(slug.ToLowerInvariant(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _ = _mockRepository.Setup(r => r.CreateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTenant);

        // Act
        Tenant result = await _tenantService.CreateTenantAsync(name, slug, description);

        // Assert
        _ = result.Should().BeEquivalentTo(expectedTenant);
        _mockRepository.Verify(r => r.CreateAsync(It.Is<Tenant>(t =>
            t.Name == name &&
            t.Slug == slug.ToLowerInvariant() &&
            t.Description == description &&
            t.IsActive == true &&
            t.IsDefault == false), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(expectedTenant.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CreateTenantAsync_Should_Throw_When_Name_Is_Invalid(string? invalidName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.CreateTenantAsync(invalidName!, "valid-slug"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task CreateTenantAsync_Should_Throw_When_Slug_Is_Invalid(string? invalidSlug)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.CreateTenantAsync("Valid Name", invalidSlug!));
    }

    #endregion

    #region UpdateTenantAsync Tests

    [Fact]
    public async Task UpdateTenantAsync_Should_Update_Tenant_Successfully()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        const string newName = "Updated Tenant";
        const string newDescription = "Updated Description";

        var existingTenant = new Tenant
        {
            Id = tenantId,
            Name = "Old Name",
            Description = "Old Description"
        };

        var updatedTenant = new Tenant
        {
            Id = tenantId,
            Name = newName,
            Description = newDescription
        };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTenant);

        _ = _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedTenant);

        // Act
        Tenant result = await _tenantService.UpdateTenantAsync(tenantId, newName, newDescription);

        // Assert
        _ = result.Should().BeEquivalentTo(updatedTenant);
        _mockRepository.Verify(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Tenant>(t =>
            t.Name == newName && t.Description == newDescription), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantAsync_Should_Throw_NotFoundException_When_Tenant_Not_Found()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _tenantService.UpdateTenantAsync(tenantId, "New Name"));

        _ = exception.Message.Should().Contain(tenantId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task UpdateTenantAsync_Should_Throw_When_Name_Is_Invalid(string? invalidName)
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _tenantService.UpdateTenantAsync(tenantId, invalidName!));
    }

    #endregion

    #region ActivateTenantAsync Tests

    [Fact]
    public async Task ActivateTenantAsync_Should_Activate_Inactive_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var inactiveTenant = new Tenant { Id = tenantId, IsActive = false };
        var activatedTenant = new Tenant { Id = tenantId, IsActive = true };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveTenant);

        _ = _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activatedTenant);

        // Act
        Tenant result = await _tenantService.ActivateTenantAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(activatedTenant);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Tenant>(t => t.IsActive), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActivateTenantAsync_Should_Return_Already_Active_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var activeTenant = new Tenant { Id = tenantId, IsActive = true };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTenant);

        // Act
        Tenant result = await _tenantService.ActivateTenantAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(activeTenant);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateTenantAsync_Should_Throw_NotFoundException_When_Tenant_Not_Found()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _tenantService.ActivateTenantAsync(tenantId));

        _ = exception.Message.Should().Contain(tenantId.ToString());
    }

    #endregion

    #region DeactivateTenantAsync Tests

    [Fact]
    public async Task DeactivateTenantAsync_Should_Deactivate_Active_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var activeTenant = new Tenant { Id = tenantId, IsActive = true, IsDefault = false };
        var deactivatedTenant = new Tenant { Id = tenantId, IsActive = false, IsDefault = false };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeTenant);

        _ = _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(deactivatedTenant);

        // Act
        Tenant result = await _tenantService.DeactivateTenantAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(deactivatedTenant);
        _mockRepository.Verify(r => r.UpdateAsync(It.Is<Tenant>(t => !t.IsActive), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateTenantAsync_Should_Throw_BusinessException_When_Default_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var defaultTenant = new Tenant { Id = tenantId, IsActive = true, IsDefault = true };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            _tenantService.DeactivateTenantAsync(tenantId));

        _ = exception.Message.Should().Contain("Cannot deactivate the default tenant");
    }

    [Fact]
    public async Task DeactivateTenantAsync_Should_Return_Already_Inactive_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var inactiveTenant = new Tenant { Id = tenantId, IsActive = false, IsDefault = false };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveTenant);

        // Act
        Tenant result = await _tenantService.DeactivateTenantAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(inactiveTenant);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(c => c.RefreshTenantAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteTenantAsync Tests

    [Fact]
    public async Task DeleteTenantAsync_Should_Delete_Non_Default_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = new Tenant { Id = tenantId, IsDefault = false };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        await _tenantService.DeleteTenantAsync(tenantId);

        // Assert
        _mockRepository.Verify(r => r.DeleteAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.InvalidateTenant(tenantId), Times.Once);
    }

    [Fact]
    public async Task DeleteTenantAsync_Should_Throw_BusinessException_When_Default_Tenant()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var defaultTenant = new Tenant { Id = tenantId, IsDefault = true };

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultTenant);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BusinessException>(() =>
            _tenantService.DeleteTenantAsync(tenantId));

        _ = exception.Message.Should().Contain("Cannot delete the default tenant");
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteTenantAsync_Should_Throw_NotFoundException_When_Tenant_Not_Found()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _tenantService.DeleteTenantAsync(tenantId));

        _ = exception.Message.Should().Contain(tenantId.ToString());
    }

    #endregion
}