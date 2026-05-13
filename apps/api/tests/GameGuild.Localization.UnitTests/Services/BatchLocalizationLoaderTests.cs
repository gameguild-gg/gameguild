using FluentAssertions;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Services;

/// <summary>
/// Tests for BatchLocalizationLoader - verifies N+1 prevention
/// </summary>
public class BatchLocalizationLoaderTests
{
    private readonly Mock<IApplicationDbContext> _mockContext;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GameGuild.Localization.BatchLocalizationLoader> _logger;

    public BatchLocalizationLoaderTests()
    {
        _mockContext = new Mock<IApplicationDbContext>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _logger = NullLogger<GameGuild.Localization.BatchLocalizationLoader>.Instance;
    }

    [Fact]
    public async Task LoadLocalizationsAsync_WithEmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        // Act
        var result = await loader.LoadLocalizationsAsync(Array.Empty<Guid>());

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadLocalizationsAsync_UsesCachedEntries_ForAllResources_WhenLanguageIsNotSpecified()
    {
        var resourceId = Guid.NewGuid();
        var cachedLocalizations = new List<GameGuild.Localization.ResourceLocalization>
        {
            new()
            {
                ResourceId = resourceId,
                LanguageId = Guid.NewGuid(),
                FieldName = "Title",
                Content = "Cached Title",
                ResourceType = "Test",
                Status = GameGuild.Localization.LocalizationStatus.Published
            }
        };

        _cache.Set($"loc:batch:{resourceId}:all", cachedLocalizations, new MemoryCacheEntryOptions { Size = 2 });
        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        var result = await loader.LoadLocalizationsAsync([resourceId]);

        result.Should().ContainKey(resourceId);
        result[resourceId].Should().BeEquivalentTo(cachedLocalizations);
        _mockContext.Verify(c => c.Set<GameGuild.Localization.ResourceLocalization>(), Times.Never);
    }

    [Fact]
    public async Task LoadLocalizationsAsync_UsesCachedEntries_ForSpecificLanguage()
    {
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var cachedLocalizations = new List<GameGuild.Localization.ResourceLocalization>
        {
            new()
            {
                ResourceId = resourceId,
                LanguageId = languageId,
                FieldName = "Title",
                Content = "Localized Title",
                ResourceType = "Test",
                Status = GameGuild.Localization.LocalizationStatus.Published
            }
        };

        _cache.Set($"loc:batch:{resourceId}:{languageId}", cachedLocalizations, new MemoryCacheEntryOptions { Size = 2 });
        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        var result = await loader.LoadLocalizationsAsync([resourceId], languageId);

        result.Should().ContainKey(resourceId);
        result[resourceId].Should().BeEquivalentTo(cachedLocalizations);
        _mockContext.Verify(c => c.Set<GameGuild.Localization.ResourceLocalization>(), Times.Never);
    }

    [Fact]
    public async Task GetLocalizedFieldAsync_CachesResult()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var fieldName = "Title";
        var defaultValue = "Default Title";

        // Setup mock to return empty (no localization found)
        var mockSet = new Mock<Microsoft.EntityFrameworkCore.DbSet<GameGuild.Localization.ResourceLocalization>>();
        _mockContext.Setup(c => c.Set<GameGuild.Localization.ResourceLocalization>()).Returns(mockSet.Object);

        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        // Pre-populate cache
        var cacheKey = $"loc:field:{resourceId}:{fieldName}:{languageId}";
        _cache.Set(cacheKey, "Cached Title");

        // Act
        var result = await loader.GetLocalizedFieldAsync(resourceId, fieldName, languageId, defaultValue);

        // Assert
        result.Should().Be("Cached Title");
    }

    [Fact]
    public async Task GetLocalizedFieldAsync_ReturnsFallback_WhenNotCached()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var fieldName = "Title";
        var defaultValue = "Default Title";

        // Pre-populate cache with null indicator
        var cacheKey = $"loc:field:{resourceId}:{fieldName}:{languageId}";
        _cache.Set(cacheKey, (string?)null);

        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        // Act
        var result = await loader.GetLocalizedFieldAsync(resourceId, fieldName, languageId, defaultValue);

        // Assert
        result.Should().Be(defaultValue);
    }

    [Fact]
    public void Constructor_ThrowsOnNullContext()
    {
        // Act & Assert
        var act = () => new GameGuild.Localization.BatchLocalizationLoader(null!, _cache, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("context");
    }

    [Fact]
    public void Constructor_ThrowsOnNullCache()
    {
        // Act & Assert
        var act = () => new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, null!, _logger);
        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act & Assert
        var act = () => new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task LoadFieldLocalizationsAsync_WithEmptyList_ReturnsEmptyDictionary()
    {
        // Arrange
        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);

        // Act
        var result = await loader.LoadFieldLocalizationsAsync(Array.Empty<Guid>(), "Title");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadFieldLocalizationsAsync_ThrowsOnNullFieldName()
    {
        // Arrange
        var loader = new GameGuild.Localization.BatchLocalizationLoader(_mockContext.Object, _cache, _logger);
        var resourceIds = new[] { Guid.NewGuid() };

        // Act & Assert
        var act = () => loader.LoadFieldLocalizationsAsync(resourceIds, null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("fieldName");
    }
}
