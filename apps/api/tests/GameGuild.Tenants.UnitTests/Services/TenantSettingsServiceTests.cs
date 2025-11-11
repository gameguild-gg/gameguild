using FluentAssertions;
using GameGuild.Localization;
using GameGuild.Modules.Tenants;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Tenants.Unit.Services;

/// <summary>
/// Unit tests for TenantSettingsService
/// </summary>
public class TenantSettingsServiceTests
{
    private readonly Mock<ITenantSettingsRepository> _mockRepository;
    private readonly Mock<ITenantCacheService> _mockCacheService;
    private readonly Mock<ILanguageRepository> _mockLanguageRepository;
    private readonly Mock<ILogger<TenantSettingsService>> _mockLogger;
    private readonly TenantSettingsService _service;

    public TenantSettingsServiceTests()
    {
        _mockRepository = new Mock<ITenantSettingsRepository>();
        _mockCacheService = new Mock<ITenantCacheService>();
        _mockLanguageRepository = new Mock<ILanguageRepository>();
        _mockLogger = new Mock<ILogger<TenantSettingsService>>();

        _service = new TenantSettingsService(
            _mockRepository.Object,
            _mockCacheService.Object,
            _mockLanguageRepository.Object,
            _mockLogger.Object
        );
    }

    #region GetTenantSettingsAsync Tests

    [Fact]
    public async Task GetTenantSettingsAsync_Should_Return_Settings_When_Found()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        TenantSettings expectedSettings = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DefaultTimezone = "UTC"
        };

        _ = _mockRepository.Setup(r => r.GetTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSettings);

        // Act
        TenantSettings? result = await _service.GetTenantSettingsAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(expectedSettings);
        _mockRepository.Verify(r => r.GetTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetTenantSettingsAsync_Should_Return_Null_When_Not_Found()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.GetTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TenantSettings?)null);

        // Act
        TenantSettings? result = await _service.GetTenantSettingsAsync(tenantId);

        // Assert
        _ = result.Should().BeNull();
        _mockRepository.Verify(r => r.GetTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region UpdateTenantSettingsAsync Tests

    [Fact]
    public async Task UpdateTenantSettingsAsync_Should_Update_Settings_Successfully()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        TenantSettings settings = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DefaultTimezone = "America/New_York"
        };

        TenantSettings updatedSettings = new()
        {
            Id = settings.Id,
            TenantId = tenantId,
            DefaultTimezone = "America/New_York"
        };

        _ = _mockRepository.Setup(r => r.CreateOrUpdateTenantSettingsAsync(tenantId, settings, It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSettings);

        // Act
        TenantSettings? result = await _service.UpdateTenantSettingsAsync(tenantId, settings);

        // Assert
        _ = result.Should().BeEquivalentTo(updatedSettings);
        _mockRepository.Verify(r => r.CreateOrUpdateTenantSettingsAsync(tenantId, settings, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateTenantSettingsAsync_Should_Throw_ArgumentNullException_When_Settings_Null()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        // Act & Assert
        _ = await Assert.ThrowsAsync<NullReferenceException>(() =>
            _service.UpdateTenantSettingsAsync(tenantId, null!));
    }

    #endregion

    #region CreateDefaultTenantSettingsAsync Tests

    [Fact]
    public async Task CreateDefaultTenantSettingsAsync_Should_Create_Default_Settings()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();
        TenantSettings expectedSettings = new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DefaultLanguageId = null,
            DefaultTimezone = "UTC"
        };

        _ = _mockRepository.Setup(r => r.CreateTenantSettingsAsync(It.IsAny<TenantSettings>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSettings);

        // Act
        TenantSettings result = await _service.CreateDefaultTenantSettingsAsync(tenantId);

        // Assert
        _ = result.Should().BeEquivalentTo(expectedSettings);
        _mockRepository.Verify(r => r.CreateTenantSettingsAsync(It.Is<TenantSettings>(s =>
            s.TenantId == tenantId &&
            s.DefaultTimezone == "UTC"), It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region DeleteTenantSettingsAsync Tests

    [Fact]
    public async Task DeleteTenantSettingsAsync_Should_Delete_Settings_Successfully()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.DeleteTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.DeleteTenantSettingsAsync(tenantId);

        // Assert
        _ = result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.InvalidateTenant(tenantId), Times.Once);
    }

    [Fact]
    public async Task DeleteTenantSettingsAsync_Should_Return_False_When_Settings_Not_Found()
    {
        // Arrange
        Guid tenantId = Guid.NewGuid();

        _ = _mockRepository.Setup(r => r.DeleteTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteTenantSettingsAsync(tenantId);

        // Assert
        _ = result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteTenantSettingsAsync(tenantId, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RefreshTenantSettingsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}