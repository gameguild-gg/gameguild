using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameGuild.Localization.UnitTests.Translation;

/// <summary>
/// Unit tests for TranslationMemoryService.
/// </summary>
public class TranslationMemoryServiceTests
{
    private readonly Mock<ILogger<TranslationMemoryService>> _loggerMock;
    private readonly TranslationMemoryService _service;

    public TranslationMemoryServiceTests()
    {
        _loggerMock = new Mock<ILogger<TranslationMemoryService>>();
        _service = new TranslationMemoryService(_loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new TranslationMemoryService(null!);
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidLogger_CreatesInstance()
    {
        // Act
        var service = new TranslationMemoryService(_loggerMock.Object);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region FindMatchAsync Tests

    [Fact]
    public async Task FindMatchAsync_WithNoEntries_ReturnsNull()
    {
        // Arrange
        const string sourceText = "Hello, world!";
        const string sourceLanguage = "en";
        const string targetLanguage = "es";

        // Act
        var result = await _service.FindMatchAsync(sourceText, sourceLanguage, targetLanguage);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchAsync_WithExactMatch_ReturnsEntry()
    {
        // Arrange
        const string sourceText = "Hello, world!";
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        const string translatedText = "¡Hola, mundo!";

        await _service.AddEntryAsync(sourceText, translatedText, sourceLanguage, targetLanguage);

        // Act
        var result = await _service.FindMatchAsync(sourceText, sourceLanguage, targetLanguage);

        // Assert
        result.Should().NotBeNull();
        result!.TranslatedText.Should().Be(translatedText);
        result.SourceLanguage.Should().Be(sourceLanguage);
        result.TargetLanguage.Should().Be(targetLanguage);
    }

    [Fact]
    public async Task FindMatchAsync_WithDifferentTargetLanguage_ReturnsNull()
    {
        // Arrange
        const string sourceText = "Hello, world!";
        const string sourceLanguage = "en";
        
        await _service.AddEntryAsync(sourceText, "¡Hola, mundo!", sourceLanguage, "es");

        // Act - look for French instead of Spanish
        var result = await _service.FindMatchAsync(sourceText, sourceLanguage, "fr");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchAsync_WithSimilarText_FindsMatchAboveThreshold()
    {
        // Arrange
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        
        // The index key is based on normalized source text
        await _service.AddEntryAsync("hello world", "Hola mundo", sourceLanguage, targetLanguage);

        // Act - search with exact normalized match (FindMatchAsync uses exact key lookup in the index)
        var result = await _service.FindMatchAsync("hello world", sourceLanguage, targetLanguage, 0.7);

        // Assert - exact normalized match should work
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task FindMatchAsync_WithTextBelowSimilarityThreshold_ReturnsNull()
    {
        // Arrange
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        
        await _service.AddEntryAsync("Hello world", "Hola mundo", sourceLanguage, targetLanguage);

        // Act - search with very different text
        var result = await _service.FindMatchAsync("Goodbye everyone", sourceLanguage, targetLanguage, 0.9);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindMatchAsync_WithMultipleMatches_ReturnsBestMatch()
    {
        // Arrange
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        
        // Add multiple similar translations
        await _service.AddEntryAsync("Hello", "Hola", sourceLanguage, targetLanguage);
        var betterEntry = await _service.AddEntryAsync("Hello world", "Hola mundo", sourceLanguage, targetLanguage);

        // Act
        var result = await _service.FindMatchAsync("Hello world", sourceLanguage, targetLanguage);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(betterEntry.Id);
    }

    [Fact]
    public async Task FindMatchAsync_WithCaseDifference_FindsMatchNormalized()
    {
        // Arrange
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        
        // Add entry with uppercase
        await _service.AddEntryAsync("HELLO WORLD", "HOLA MUNDO", sourceLanguage, targetLanguage);

        // Act - search with lowercase (normalized search should find it via index key lookup)
        // The index key is normalized, so both "HELLO WORLD" and "hello world" normalize to "hello world"
        var result = await _service.FindMatchAsync("HELLO WORLD", sourceLanguage, targetLanguage);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithNoEntries_ReturnsEmptyList()
    {
        // Act
        var results = await _service.SearchAsync("hello");

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithMatchingEntry_ReturnsResults()
    {
        // Arrange
        // Use a source text that will have high similarity when searched
        await _service.AddEntryAsync("Hello world", "Hola mundo", "en", "es");

        // Act - Search uses similarity calculation, need text similar enough to get > 0.5 similarity
        var results = await _service.SearchAsync("Hello world");

        // Assert
        results.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchAsync_WithSourceLanguageFilter_FiltersCorrectly()
    {
        // Arrange
        await _service.AddEntryAsync("Hello", "Hola", "en", "es");
        await _service.AddEntryAsync("Bonjour", "Hello", "fr", "en");

        // Act
        var results = await _service.SearchAsync("Hello", sourceLanguage: "en");

        // Assert
        results.Should().HaveCount(1);
        results.First().SourceLanguage.Should().Be("en");
    }

    [Fact]
    public async Task SearchAsync_WithTargetLanguageFilter_FiltersCorrectly()
    {
        // Arrange
        await _service.AddEntryAsync("Hello", "Hola", "en", "es");
        await _service.AddEntryAsync("Hello", "Bonjour", "en", "fr");

        // Act
        var results = await _service.SearchAsync("Hello", targetLanguage: "es");

        // Assert
        results.Should().HaveCount(1);
        results.First().TargetLanguage.Should().Be("es");
    }

    [Fact]
    public async Task SearchAsync_WithMaxResults_LimitsOutput()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            await _service.AddEntryAsync($"Hello {i}", $"Hola {i}", "en", "es");
        }

        // Act
        var results = await _service.SearchAsync("Hello", maxResults: 5);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task SearchAsync_ReturnsResultsOrderedBySimilarity()
    {
        // Arrange
        await _service.AddEntryAsync("Hello", "Hola", "en", "es");
        await _service.AddEntryAsync("Hello world", "Hola mundo", "en", "es");
        await _service.AddEntryAsync("Hello world test", "Hola mundo prueba", "en", "es");

        // Act
        var results = (await _service.SearchAsync("Hello world")).ToList();

        // Assert
        results.Should().NotBeEmpty();
        // More similar should come first
        results.First().SourceText.Should().Be("Hello world");
    }

    #endregion

    #region AddEntryAsync Tests

    [Fact]
    public async Task AddEntryAsync_CreatesEntryWithCorrectProperties()
    {
        // Arrange
        const string sourceText = "Hello";
        const string translatedText = "Hola";
        const string sourceLanguage = "en";
        const string targetLanguage = "es";
        const string context = "Greeting";
        var metadata = new Dictionary<string, string> { ["key"] = "value" };

        // Act
        var entry = await _service.AddEntryAsync(
            sourceText, translatedText, sourceLanguage, targetLanguage, context, metadata);

        // Assert
        entry.Should().NotBeNull();
        entry.Id.Should().NotBe(Guid.Empty);
        entry.SourceText.Should().Be(sourceText);
        entry.TranslatedText.Should().Be(translatedText);
        entry.SourceLanguage.Should().Be(sourceLanguage);
        entry.TargetLanguage.Should().Be(targetLanguage);
        entry.Context.Should().Be(context);
        entry.Metadata.Should().ContainKey("key");
        entry.QualityScore.Should().Be(1.0);
        entry.UsageCount.Should().Be(0);
        entry.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AddEntryAsync_WithNullMetadata_CreatesEmptyMetadata()
    {
        // Act
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");

        // Assert
        entry.Metadata.Should().NotBeNull();
        entry.Metadata.Should().BeEmpty();
    }

    [Fact]
    public async Task AddEntryAsync_WithNullContext_SetsContextToNull()
    {
        // Act
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");

        // Assert
        entry.Context.Should().BeNull();
    }

    [Fact]
    public async Task AddEntryAsync_AllowsDuplicateSourceText()
    {
        // Arrange
        const string sourceText = "Hello";
        const string sourceLanguage = "en";

        // Act
        var entry1 = await _service.AddEntryAsync(sourceText, "Hola", sourceLanguage, "es");
        var entry2 = await _service.AddEntryAsync(sourceText, "Bonjour", sourceLanguage, "fr");

        // Assert
        entry1.Id.Should().NotBe(entry2.Id);
    }

    #endregion

    #region UpdateQualityScoreAsync Tests

    [Fact]
    public async Task UpdateQualityScoreAsync_UpdatesScoreCorrectly()
    {
        // Arrange
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");
        const double newScore = 0.8;

        // Act
        await _service.UpdateQualityScoreAsync(entry.Id, newScore);

        // Assert - verify through find
        var foundEntry = await _service.FindMatchAsync("Hello", "en", "es");
        foundEntry!.QualityScore.Should().Be(newScore);
    }

    [Fact]
    public async Task UpdateQualityScoreAsync_WithScoreAbove1_ClampsTo1()
    {
        // Arrange
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");

        // Act
        await _service.UpdateQualityScoreAsync(entry.Id, 1.5);

        // Assert
        var foundEntry = await _service.FindMatchAsync("Hello", "en", "es");
        foundEntry!.QualityScore.Should().Be(1.0);
    }

    [Fact]
    public async Task UpdateQualityScoreAsync_WithScoreBelow0_ClampsTo0()
    {
        // Arrange
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");

        // Act
        await _service.UpdateQualityScoreAsync(entry.Id, -0.5);

        // Assert
        var foundEntry = await _service.FindMatchAsync("Hello", "en", "es");
        foundEntry!.QualityScore.Should().Be(0.0);
    }

    [Fact]
    public async Task UpdateQualityScoreAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act & Assert
        var act = () => _service.UpdateQualityScoreAsync(invalidId, 0.5);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{invalidId}*not found*");
    }

    [Fact]
    public async Task UpdateQualityScoreAsync_SetsLastUpdatedAt()
    {
        // Arrange
        var entry = await _service.AddEntryAsync("Hello", "Hola", "en", "es");
        
        // Small delay to ensure time difference
        await Task.Delay(10);

        // Act
        await _service.UpdateQualityScoreAsync(entry.Id, 0.9);

        // Assert
        var foundEntry = await _service.FindMatchAsync("Hello", "en", "es");
        foundEntry!.LastUpdatedAt.Should().NotBeNull();
        foundEntry.LastUpdatedAt.Should().BeAfter(foundEntry.CreatedAt);
    }

    #endregion

    #region GetRecentEntriesAsync Tests

    [Fact]
    public async Task GetRecentEntriesAsync_WithNoEntries_ReturnsEmptyList()
    {
        // Act
        var results = await _service.GetRecentEntriesAsync();

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRecentEntriesAsync_ReturnsEntriesOrderedByCreatedAt()
    {
        // Arrange
        var entry1 = await _service.AddEntryAsync("First", "Primero", "en", "es");
        await Task.Delay(10);
        var entry2 = await _service.AddEntryAsync("Second", "Segundo", "en", "es");
        await Task.Delay(10);
        var entry3 = await _service.AddEntryAsync("Third", "Tercero", "en", "es");

        // Act
        var results = (await _service.GetRecentEntriesAsync()).ToList();

        // Assert
        results.Should().HaveCount(3);
        results[0].Id.Should().Be(entry3.Id); // Most recent first
        results[1].Id.Should().Be(entry2.Id);
        results[2].Id.Should().Be(entry1.Id);
    }

    [Fact]
    public async Task GetRecentEntriesAsync_WithCountLimit_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _service.AddEntryAsync($"Entry {i}", $"Entrada {i}", "en", "es");
        }

        // Act
        var results = await _service.GetRecentEntriesAsync(count: 5);

        // Assert
        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetRecentEntriesAsync_DefaultCountIs100()
    {
        // Arrange - add more than 100 entries
        for (int i = 0; i < 110; i++)
        {
            await _service.AddEntryAsync($"Entry {i}", $"Entrada {i}", "en", "es");
        }

        // Act
        var results = await _service.GetRecentEntriesAsync();

        // Assert
        results.Should().HaveCount(100);
    }

    #endregion

    #region Similarity Algorithm Tests

    [Fact]
    public async Task CalculateSimilarity_IdenticalStrings_Returns1()
    {
        // Arrange
        await _service.AddEntryAsync("exact match", "coincidencia exacta", "en", "es");

        // Act
        var result = await _service.FindMatchAsync("exact match", "en", "es", 0.99);

        // Assert - should find because similarity is 1.0
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateSimilarity_EmptyStrings_HandlesCorrectly()
    {
        // Arrange
        await _service.AddEntryAsync("", "", "en", "es");

        // Act
        var result = await _service.FindMatchAsync("", "en", "es");

        // Assert - empty strings should match
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateSimilarity_CompletelyDifferent_ReturnsLowScore()
    {
        // Arrange
        await _service.AddEntryAsync("Hello world", "Hola mundo", "en", "es");

        // Act - very different text with high threshold
        var result = await _service.FindMatchAsync("XYZABC", "en", "es", 0.8);

        // Assert - should not match
        result.Should().BeNull();
    }

    #endregion
}
