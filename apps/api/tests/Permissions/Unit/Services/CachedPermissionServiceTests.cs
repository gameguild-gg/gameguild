using AutoFixture;
using AutoFixture.Xunit2;
using FluentAssertions;
using GameGuild.Modules.Permissions.Services;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Permissions.Unit.Services;

/// <summary>
/// Unit tests for CachedPermissionService
/// </summary>
public class CachedPermissionServiceTests
{
    private readonly Mock<IPermissionService> _mockInnerService;
    private readonly Mock<IMemoryCache> _mockCache;
    private readonly Mock<ILogger<CachedPermissionService>> _mockLogger;
    private readonly CachedPermissionService _sut;
    private readonly Fixture _fixture;

    public CachedPermissionServiceTests()
    {
        _mockInnerService = new Mock<IPermissionService>();
        _mockCache = new Mock<IMemoryCache>();
        _mockLogger = new Mock<ILogger<CachedPermissionService>>();
        _sut = new CachedPermissionService(_mockInnerService.Object, _mockCache.Object, _mockLogger.Object);
        _fixture = new Fixture();
    }

    [Theory]
    [AutoData]
    public async Task HasTenantPermissionAsync_WhenCacheHit_ShouldReturnCachedValue(
        Guid userId, Guid tenantId, PermissionType permission, bool expectedResult)
    {
        // Arrange
        var cacheKey = $"tenant:{userId}:{tenantId}:{permission}";
        object cachedValue = expectedResult;
        
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(true);

        // Act
        var result = await _sut.HasTenantPermissionAsync(userId, tenantId, permission);

        // Assert
        result.Should().Be(expectedResult);
        _mockInnerService.Verify(x => x.HasTenantPermissionAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<PermissionType>()), Times.Never);
    }

    [Theory]
    [AutoData]
    public async Task HasTenantPermissionAsync_WhenCacheMiss_ShouldCallInnerServiceAndCache(
        Guid userId, Guid tenantId, PermissionType permission, bool expectedResult)
    {
        // Arrange
        object cachedValue = null!;
        _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cachedValue))
            .Returns(false);
        
        _mockInnerService.Setup(x => x.HasTenantPermissionAsync(userId, tenantId, permission))
            .ReturnsAsync(expectedResult);

        var mockCacheEntry = new Mock<ICacheEntry>();
        _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>()))
            .Returns(mockCacheEntry.Object);

        // Act
        var result = await _sut.HasTenantPermissionAsync(userId, tenantId, permission);

        // Assert
        result.Should().Be(expectedResult);
        _mockInnerService.Verify(x => x.HasTenantPermissionAsync(userId, tenantId, permission), Times.Once);
        _mockCache.Verify(x => x.CreateEntry(It.IsAny<object>()), Times.Once);
    }

    [Theory]
    [AutoData]
    public async Task InvalidateUserPermissionCacheAsync_ShouldRemoveCacheEntries(
        Guid userId, Guid tenantId)
    {
        // Act
        await _sut.InvalidateUserPermissionCacheAsync(userId, tenantId);

        // Assert
        _mockCache.Verify(x => x.Remove(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetCacheStatisticsAsync_ShouldReturnStatistics()
    {
        // Act
        var result = await _sut.GetCacheStatisticsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<GameGuild.Modules.Permissions.Models.PermissionCacheStatistics>();
    }

    [Fact]
    public void Constructor_WithNullInnerService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CachedPermissionService(null!, _mockCache.Object, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("innerService");
    }

    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CachedPermissionService(_mockInnerService.Object, null!, _mockLogger.Object);
        action.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new CachedPermissionService(_mockInnerService.Object, _mockCache.Object, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}