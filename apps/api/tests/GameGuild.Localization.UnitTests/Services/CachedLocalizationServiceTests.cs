using FluentAssertions;
using GameGuild.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Services;

/// <summary>
/// Tests for CachedLocalizationService to verify caching behavior.
/// </summary>
public class CachedLocalizationServiceTests
{
    private readonly Mock<ILocalizationService> _innerServiceMock;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<CachedLocalizationService>> _loggerMock;
    private readonly CachedLocalizationService _service;

    public CachedLocalizationServiceTests()
    {
        _innerServiceMock = new Mock<ILocalizationService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<CachedLocalizationService>>();
        _service = new CachedLocalizationService(_innerServiceMock.Object, _cache, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerService()
    {
        var act = () => new CachedLocalizationService(null!, _cache, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("inner");
    }

    [Fact]
    public void Constructor_ThrowsOnNullCache()
    {
        var act = () => new CachedLocalizationService(_innerServiceMock.Object, null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("cache");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        var act = () => new CachedLocalizationService(_innerServiceMock.Object, _cache, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetLocalizationAsync_CachesResult_OnFirstCall()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var localization = CreateLocalization(resourceId, languageId);

        _innerServiceMock
            .Setup(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);

        // Act
        var result1 = await _service.GetLocalizationAsync(resourceId, languageId);
        var result2 = await _service.GetLocalizationAsync(resourceId, languageId);

        // Assert
        result1.Should().Be(localization);
        result2.Should().Be(localization);
        _innerServiceMock.Verify(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLocalizationAsync_DoesNotCacheNull()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();

        _innerServiceMock
            .Setup(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResourceLocalization?)null);

        // Act
        var result1 = await _service.GetLocalizationAsync(resourceId, languageId);
        var result2 = await _service.GetLocalizationAsync(resourceId, languageId);

        // Assert
        result1.Should().BeNull();
        result2.Should().BeNull();
        _innerServiceMock.Verify(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetAllLocalizationsAsync_CachesResult()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var localizations = new List<ResourceLocalization>
        {
            CreateLocalization(resourceId, Guid.NewGuid()),
            CreateLocalization(resourceId, Guid.NewGuid())
        };

        _innerServiceMock
            .Setup(x => x.GetAllLocalizationsAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localizations);

        // Act
        var result1 = await _service.GetAllLocalizationsAsync(resourceId);
        var result2 = await _service.GetAllLocalizationsAsync(resourceId);

        // Assert
        result1.Should().BeEquivalentTo(localizations);
        result2.Should().BeEquivalentTo(localizations);
        _innerServiceMock.Verify(x => x.GetAllLocalizationsAsync(resourceId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLocalizationAsync_InvalidatesCache()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var localization = CreateLocalization(resourceId, languageId);

        _innerServiceMock
            .Setup(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);
        _innerServiceMock
            .Setup(x => x.CreateLocalizationAsync(It.IsAny<ResourceLocalization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);

        // Populate cache
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Act - Create invalidates cache
        await _service.CreateLocalizationAsync(localization);

        // Subsequent get should call inner service again
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Assert
        _innerServiceMock.Verify(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateLocalizationAsync_InvalidatesCache()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var localization = CreateLocalization(resourceId, languageId);

        _innerServiceMock
            .Setup(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);
        _innerServiceMock
            .Setup(x => x.UpdateLocalizationAsync(It.IsAny<ResourceLocalization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);

        // Populate cache
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Act - Update invalidates cache
        await _service.UpdateLocalizationAsync(localization);

        // Subsequent get should call inner service again
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Assert
        _innerServiceMock.Verify(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteLocalizationAsync_InvalidatesCache()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var localization = CreateLocalization(resourceId, languageId);

        _innerServiceMock
            .Setup(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);
        _innerServiceMock
            .Setup(x => x.DeleteLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Populate cache
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Act - Delete invalidates cache
        await _service.DeleteLocalizationAsync(resourceId, languageId);

        // Subsequent get should call inner service again
        await _service.GetLocalizationAsync(resourceId, languageId);

        // Assert
        _innerServiceMock.Verify(x => x.GetLocalizationAsync(resourceId, languageId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task GetLocalizationsForFieldAsync_CachesResult()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var fieldName = "Title";
        var localizations = new List<ResourceLocalization>
        {
            CreateLocalization(resourceId, Guid.NewGuid(), fieldName)
        };

        _innerServiceMock
            .Setup(x => x.GetLocalizationsForFieldAsync(resourceId, fieldName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(localizations);

        // Act
        var result1 = await _service.GetLocalizationsForFieldAsync(resourceId, fieldName);
        var result2 = await _service.GetLocalizationsForFieldAsync(resourceId, fieldName);

        // Assert
        result1.Should().BeEquivalentTo(localizations);
        result2.Should().BeEquivalentTo(localizations);
        _innerServiceMock.Verify(x => x.GetLocalizationsForFieldAsync(resourceId, fieldName, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateLocalizationAsync_InvalidatesAllLocalizationsCache()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();
        var localization = CreateLocalization(resourceId, languageId);
        var allLocalizations = new List<ResourceLocalization> { localization };

        _innerServiceMock
            .Setup(x => x.GetAllLocalizationsAsync(resourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allLocalizations);
        _innerServiceMock
            .Setup(x => x.CreateLocalizationAsync(It.IsAny<ResourceLocalization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(localization);

        // Populate "all localizations" cache
        await _service.GetAllLocalizationsAsync(resourceId);

        // Act - Create invalidates cache
        await _service.CreateLocalizationAsync(localization);

        // Subsequent get should call inner service again
        await _service.GetAllLocalizationsAsync(resourceId);

        // Assert
        _innerServiceMock.Verify(x => x.GetAllLocalizationsAsync(resourceId, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static ResourceLocalization CreateLocalization(Guid resourceId, Guid languageId, string? fieldName = null)
    {
        return new ResourceLocalization
        {
            ResourceId = resourceId,
            LanguageId = languageId,
            FieldName = fieldName ?? "Title",
            Content = "Test Value",
            ResourceType = "Test",
            Status = LocalizationStatus.Draft
        };
    }
}
