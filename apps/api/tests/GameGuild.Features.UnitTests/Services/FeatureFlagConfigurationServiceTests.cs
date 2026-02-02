using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace GameGuild.Features.UnitTests.Services;

public class FeatureFlagConfigurationServiceTests
{
    private readonly Mock<IFeatureFlagQueryRepository> _queryRepositoryMock;
    private readonly Mock<ILogger<FeatureFlagConfigurationService>> _loggerMock;
    private readonly FeatureFlagOptions _options;
    private readonly FeatureFlagConfigurationService _serviceWithoutCache;
    private readonly FeatureFlagConfigurationService _serviceWithCache;
    private readonly MemoryCache _memoryCache;

    public FeatureFlagConfigurationServiceTests()
    {
        _queryRepositoryMock = new Mock<IFeatureFlagQueryRepository>();
        _loggerMock = new Mock<ILogger<FeatureFlagConfigurationService>>();
        _options = new FeatureFlagOptions
        {
            DefaultEnvironment = "production",
            CacheTtlMinutes = 5,
            EnableCaching = false
        };

        // Service without cache for most tests (simpler)
        _serviceWithoutCache = new FeatureFlagConfigurationService(
            _queryRepositoryMock.Object,
            _loggerMock.Object,
            Options.Create(_options),
            null);
        
        // Service with cache for caching-specific tests
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        var optionsWithCaching = new FeatureFlagOptions
        {
            DefaultEnvironment = "production",
            CacheTtlMinutes = 5,
            EnableCaching = true
        };
        _serviceWithCache = new FeatureFlagConfigurationService(
            _queryRepositoryMock.Object,
            _loggerMock.Object,
            Options.Create(optionsWithCaching),
            _memoryCache);
    }

    #region GetConfigAsync Tests

    [Fact]
    public async Task GetConfigAsync_FeatureNotFound_ReturnsNull()
    {
        // Arrange
        var featureKey = "non-existent-feature";
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FeatureFlag?)null);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigAsync_FeatureExists_ReturnsConfig()
    {
        // Arrange
        var featureKey = "test-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey);

        // Assert
        result.Should().NotBeNull();
        result!.Key.Should().Be(featureKey);
    }

    [Fact]
    public async Task GetConfigAsync_ReturnsCorrectIsEnabledValue()
    {
        // Arrange
        var featureKey = "enabled-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigAsync_DisabledFeature_ReturnsCorrectStatus()
    {
        // Arrange
        var featureKey = "disabled-feature";
        var featureFlag = CreateFeatureFlag(featureKey, false);
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey);

        // Assert
        result.Should().NotBeNull();
        result!.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigAsync_WrongEnvironment_ReturnsNull()
    {
        // Arrange
        var featureKey = "test-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        featureFlag.Environment = "staging";
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act - requesting production environment but flag is staging
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey, "production");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigAsync_MatchingEnvironment_ReturnsConfig()
    {
        // Arrange
        var featureKey = "test-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        featureFlag.Environment = "staging";
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey, "staging");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConfigAsync_NullEnvironment_UsesDefault()
    {
        // Arrange
        var featureKey = "test-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result = await _serviceWithoutCache.GetConfigAsync(featureKey, environment: null);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConfigAsync_EmptyFeatureKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _serviceWithoutCache.GetConfigAsync(string.Empty));
    }

    [Fact]
    public async Task GetConfigAsync_WhitespaceFeatureKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _serviceWithoutCache.GetConfigAsync("   "));
    }

    #endregion

    #region GetAllConfigsAsync Tests

    [Fact]
    public async Task GetAllConfigsAsync_ReturnsAllConfigs()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true),
            CreateFeatureFlag("feature-2", false)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.GetAllConfigsAsync();

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllConfigsAsync_EmptyList_ReturnsEmptyCollection()
    {
        // Arrange
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FeatureFlag>());

        // Act
        var result = await _serviceWithoutCache.GetAllConfigsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllConfigsAsync_WithEnvironment_FiltersCorrectly()
    {
        // Arrange
        var environment = "staging";
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("staging-feature", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync(environment, It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.GetAllConfigsAsync(environment);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllConfigsAsync_MapsAllProperties()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = (await _serviceWithoutCache.GetAllConfigsAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Key.Should().Be("feature-1");
        result[0].IsEnabled.Should().BeTrue();
    }

    #endregion

    #region GetConfigsAsync Tests

    [Fact]
    public async Task GetConfigsAsync_MultipleKeys_ReturnsBatch()
    {
        // Arrange
        var keys = new[] { "feature-1", "feature-2" };
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true),
            CreateFeatureFlag("feature-2", false)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeysAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.GetConfigsAsync(keys);

        // Assert
        result.Should().HaveCount(2);
        result.ContainsKey("feature-1").Should().BeTrue();
        result.ContainsKey("feature-2").Should().BeTrue();
    }

    [Fact]
    public async Task GetConfigsAsync_EmptyKeys_ReturnsEmptyDictionary()
    {
        // Arrange
        var keys = Array.Empty<string>();

        // Act
        var result = await _serviceWithoutCache.GetConfigsAsync(keys);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetConfigsAsync_NullKeys_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _serviceWithoutCache.GetConfigsAsync(null!));
    }

    [Fact]
    public async Task GetConfigsAsync_FiltersEnvironmentCorrectly()
    {
        // Arrange
        var keys = new[] { "feature-1", "feature-2" };
        var flag1 = CreateFeatureFlag("feature-1", true);
        flag1.Environment = "production";
        var flag2 = CreateFeatureFlag("feature-2", true);
        flag2.Environment = "staging";
        var flags = new List<FeatureFlag> { flag1, flag2 };
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeysAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.GetConfigsAsync(keys, "production");

        // Assert
        result.Should().HaveCount(1);
        result.ContainsKey("feature-1").Should().BeTrue();
    }

    #endregion

    #region Hash Generation Tests

    [Fact]
    public async Task GetConfigHashAsync_ReturnsNonEmptyHash()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.GetConfigHashAsync();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetConfigHashAsync_SameConfigs_SameHash()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var hash1 = await _serviceWithoutCache.GetConfigHashAsync();
        var hash2 = await _serviceWithoutCache.GetConfigHashAsync();

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task HasConfigChangedAsync_SameHash_ReturnsFalse()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        var hash = await _serviceWithoutCache.GetConfigHashAsync();

        // Act
        var result = await _serviceWithoutCache.HasConfigChangedAsync(hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasConfigChangedAsync_DifferentHash_ReturnsTrue()
    {
        // Arrange
        var flags = new List<FeatureFlag>
        {
            CreateFeatureFlag("feature-1", true)
        };
        
        _queryRepositoryMock
            .Setup(x => x.GetByEnvironmentAsync("production", It.IsAny<CancellationToken>()))
            .ReturnsAsync(flags);

        // Act
        var result = await _serviceWithoutCache.HasConfigChangedAsync("different-hash");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasConfigChangedAsync_EmptyHash_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _serviceWithoutCache.HasConfigChangedAsync(string.Empty));
    }

    #endregion

    #region Caching Tests

    [Fact]
    public async Task GetConfigAsync_WithCaching_CachesResult()
    {
        // Arrange
        var featureKey = "cached-feature";
        var featureFlag = CreateFeatureFlag(featureKey, true);
        
        _queryRepositoryMock
            .Setup(x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(featureFlag);

        // Act
        var result1 = await _serviceWithCache.GetConfigAsync(featureKey);
        var result2 = await _serviceWithCache.GetConfigAsync(featureKey);

        // Assert
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
        // Repository should only be called once due to caching
        _queryRepositoryMock.Verify(
            x => x.GetByKeyAsync(featureKey, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static FeatureFlag CreateFeatureFlag(string key, bool isEnabled)
    {
        return new FeatureFlag
        {
            Id = Guid.NewGuid(),
            Key = key,
            Name = $"Test flag {key}",
            IsEnabled = isEnabled,
            Type = FeatureFlagType.Toggle,
            DefaultValue = "false",
            EnabledValue = "true"
        };
    }

    #endregion
}
