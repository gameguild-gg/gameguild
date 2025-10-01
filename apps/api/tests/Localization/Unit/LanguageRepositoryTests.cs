using FluentAssertions;
using GameGuild.Database;
using GameGuild.Modules.Localization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Unit tests for LanguageRepository
/// </summary>
public class LanguageRepositoryTests : IAsyncDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly LanguageRepository _repository;

    public LanguageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new LanguageRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetDefaultAsync_Should_Return_Default_Language_When_Exists()
    {
        // Arrange
        var defaultLanguage = new Language { Code = "en-US", Name = "English", IsDefault = true, IsActive = true };
        var otherLanguage = new Language { Code = "es-ES", Name = "Spanish", IsDefault = false, IsActive = true };

        _context.Languages.AddRange(defaultLanguage, otherLanguage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(defaultLanguage.Id);
        result.Code.Should().Be("en-US");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetDefaultAsync_Should_Return_Null_When_No_Default_Language_Exists()
    {
        // Arrange
        var language1 = new Language { Code = "es-ES", Name = "Spanish", IsDefault = false, IsActive = true };
        var language2 = new Language { Code = "fr-FR", Name = "French", IsDefault = false, IsActive = true };

        _context.Languages.AddRange(language1, language2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetDefaultAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetDefaultAsync_Should_Return_Null_When_No_Languages_Exist()
    {
        // Act
        var result = await _repository.GetDefaultAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Language_When_Exists()
    {
        // Arrange
        var language = new Language { Code = "de-DE", Name = "German", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(language.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(language.Id);
        result.Code.Should().Be("de-DE");
        result.Name.Should().Be("German");
    }

    [Fact]
    public async Task GetByIdAsync_Should_Return_Null_When_Language_Does_Not_Exist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Return_Language_When_Exists()
    {
        // Arrange
        var language = new Language { Code = "pt-BR", Name = "Portuguese (Brazil)", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByCodeAsync("pt-BR");

        // Assert
        result.Should().NotBeNull();
        result!.Code.Should().Be("pt-BR");
        result.Name.Should().Be("Portuguese (Brazil)");
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Return_Null_When_Language_Does_Not_Exist()
    {
        // Act
        var result = await _repository.GetByCodeAsync("xx-XX");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public async Task GetByCodeAsync_Should_Throw_ArgumentException_For_Invalid_Code(string invalidCode)
    {
        // Act & Assert
        await FluentActions.Invoking(() => _repository.GetByCodeAsync(invalidCode))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetActiveAsync_Should_Return_Only_Active_Languages()
    {
        // Arrange
        var activeLanguage1 = new Language { Code = "en-US", Name = "English", IsDefault = true, IsActive = true };
        var activeLanguage2 = new Language { Code = "es-ES", Name = "Spanish", IsDefault = false, IsActive = true };
        var inactiveLanguage = new Language { Code = "fr-FR", Name = "French", IsDefault = false, IsActive = false };

        _context.Languages.AddRange(activeLanguage1, activeLanguage2, inactiveLanguage);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Code == "en-US");
        result.Should().Contain(l => l.Code == "es-ES");
        result.Should().NotContain(l => l.Code == "fr-FR");

        // Verify they are ordered by name
        result.Should().BeInAscendingOrder(l => l.Name);
    }

    [Fact]
    public async Task GetActiveAsync_Should_Return_Empty_List_When_No_Active_Languages()
    {
        // Arrange
        var inactiveLanguage1 = new Language { Code = "de-DE", Name = "German", IsDefault = false, IsActive = false };
        var inactiveLanguage2 = new Language { Code = "it-IT", Name = "Italian", IsDefault = false, IsActive = false };

        _context.Languages.AddRange(inactiveLanguage1, inactiveLanguage2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetActiveAsync_Should_Return_Languages_Ordered_By_Name()
    {
        // Arrange
        var languageZ = new Language { Code = "zh-CN", Name = "Chinese", IsDefault = false, IsActive = true };
        var languageA = new Language { Code = "ar-SA", Name = "Arabic", IsDefault = false, IsActive = true };
        var languageM = new Language { Code = "ja-JP", Name = "Japanese", IsDefault = false, IsActive = true };

        _context.Languages.AddRange(languageZ, languageA, languageM);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        result.Should().HaveCount(3);
        result[0].Name.Should().Be("Arabic");
        result[1].Name.Should().Be("Chinese");
        result[2].Name.Should().Be("Japanese");
    }

    [Fact]
    public async Task GetActiveAsync_Should_Return_ReadOnly_List()
    {
        // Arrange
        var language = new Language { Code = "ko-KR", Name = "Korean", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActiveAsync();

        // Assert
        result.Should().BeAssignableTo<IReadOnlyList<Language>>();
    }

    [Fact]
    public async Task Repository_Should_Use_NoTracking_For_Read_Operations()
    {
        // Arrange
        var language = new Language { Code = "sv-SE", Name = "Swedish", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(language.Id);

        // Assert
        result.Should().NotBeNull();

        // Verify that the entity is not being tracked
        var entityEntry = _context.Entry(result!);
        entityEntry.State.Should().Be(EntityState.Detached);
    }

    [Fact]
    public async Task Multiple_Operations_Should_Work_Independently()
    {
        // Arrange
        var defaultLanguage = new Language { Code = "en-US", Name = "English", IsDefault = true, IsActive = true };
        var spanishLanguage = new Language { Code = "es-ES", Name = "Spanish", IsDefault = false, IsActive = true };
        var inactiveLanguage = new Language { Code = "fr-FR", Name = "French", IsDefault = false, IsActive = false };

        _context.Languages.AddRange(defaultLanguage, spanishLanguage, inactiveLanguage);
        await _context.SaveChangesAsync();

        // Act
        var defaultResult = await _repository.GetDefaultAsync();
        var byIdResult = await _repository.GetByIdAsync(spanishLanguage.Id);
        var byCodeResult = await _repository.GetByCodeAsync("es-ES");
        var activeResults = await _repository.GetActiveAsync();

        // Assert
        defaultResult.Should().NotBeNull();
        defaultResult!.IsDefault.Should().BeTrue();

        byIdResult.Should().NotBeNull();
        byIdResult!.Code.Should().Be("es-ES");

        byCodeResult.Should().NotBeNull();
        byCodeResult!.Code.Should().Be("es-ES");

        activeResults.Should().HaveCount(2);
        activeResults.Should().NotContain(l => l.Code == "fr-FR");
    }

    [Fact]
    public async Task Repository_Should_Handle_Cancellation_Token()
    {
        // Arrange
        var language = new Language { Code = "da-DK", Name = "Danish", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        using var cts = new CancellationTokenSource();

        // Act & Assert - Should not throw when cancellation token is provided
        var defaultResult = await _repository.GetDefaultAsync(cts.Token);
        var byIdResult = await _repository.GetByIdAsync(language.Id, cts.Token);
        var byCodeResult = await _repository.GetByCodeAsync("da-DK", cts.Token);
        var activeResults = await _repository.GetActiveAsync(cts.Token);

        // Verify results
        byCodeResult.Should().NotBeNull();
        byCodeResult!.Code.Should().Be("da-DK");
        activeResults.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetByCodeAsync_Should_Be_Case_Sensitive()
    {
        // Arrange
        var language = new Language { Code = "nl-NL", Name = "Dutch", IsDefault = false, IsActive = true };
        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        // Act
        var correctCaseResult = await _repository.GetByCodeAsync("nl-NL");
        var wrongCaseResult = await _repository.GetByCodeAsync("NL-NL");

        // Assert
        correctCaseResult.Should().NotBeNull();
        wrongCaseResult.Should().BeNull();
    }
}