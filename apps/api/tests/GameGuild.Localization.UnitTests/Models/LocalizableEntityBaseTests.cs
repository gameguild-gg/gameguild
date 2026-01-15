using FluentAssertions;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Models;

/// <summary>
/// Tests for LocalizableEntityBase abstract class
/// </summary>
public class LocalizableEntityBaseTests
{
    // Test implementation of LocalizableEntityBase
    private class TestLocalizableEntity : GameGuild.Localization.LocalizableEntityBase<GameGuild.Localization.ResourceLocalization>
    {
        public string Name { get; set; } = string.Empty;
    }

    private static GameGuild.Localization.Language CreateTestLanguage(string code = "en-US")
    {
        return new GameGuild.Localization.Language
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code == "en-US" ? "English" : "Spanish",
            IsActive = true
        };
    }

    [Fact]
    public void AddLocalization_AddsToCollection()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.AddLocalization("Title", "Test Title", language);

        // Assert
        result.Should().NotBeNull();
        entity.Localizations.Should().ContainSingle();
        result.FieldName.Should().Be("Title");
        result.Content.Should().Be("Test Title");
        result.LanguageId.Should().Be(language.Id);
        result.Status.Should().Be(GameGuild.Localization.LocalizationStatus.Draft);
    }

    [Fact]
    public void AddLocalization_UsesProvidedStatus()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.AddLocalization("Title", "Test Title", language, 
            GameGuild.Localization.LocalizationStatus.Published);

        // Assert
        result.Status.Should().Be(GameGuild.Localization.LocalizationStatus.Published);
    }

    [Fact]
    public void AddLocalization_ThrowsOnNullFieldName()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act & Assert
        var act = () => entity.AddLocalization(null!, "content", language);
        act.Should().Throw<ArgumentNullException>().WithParameterName("fieldName");
    }

    [Fact]
    public void AddLocalization_ThrowsOnNullContent()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act & Assert
        var act = () => entity.AddLocalization("Title", null!, language);
        act.Should().Throw<ArgumentNullException>().WithParameterName("content");
    }

    [Fact]
    public void AddLocalization_ThrowsOnNullLanguage()
    {
        // Arrange
        var entity = new TestLocalizableEntity();

        // Act & Assert
        var act = () => entity.AddLocalization("Title", "content", null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("language");
    }

    [Fact]
    public void GetLocalization_ReturnsExisting()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Test Title", language);

        // Act
        var result = entity.GetLocalization("Title", language);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Test Title");
    }

    [Fact]
    public void GetLocalization_ReturnsNull_WhenNotFound()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.GetLocalization("Title", language);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLocalization_ReturnsNull_WhenDifferentField()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Test Title", language);

        // Act
        var result = entity.GetLocalization("Description", language);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLocalization_ReturnsNull_WhenDifferentLanguage()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var english = CreateTestLanguage("en-US");
        var spanish = CreateTestLanguage("es-ES");
        entity.AddLocalization("Title", "Test Title", english);

        // Act
        var result = entity.GetLocalization("Title", spanish);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLocalizationsForField_ReturnsAllForField()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var english = CreateTestLanguage("en-US");
        var spanish = CreateTestLanguage("es-ES");
        entity.AddLocalization("Title", "Test Title", english);
        entity.AddLocalization("Title", "Título de prueba", spanish);
        entity.AddLocalization("Description", "Some description", english);

        // Act
        var results = entity.GetLocalizationsForField("Title").ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(l => l.Content == "Test Title");
        results.Should().Contain(l => l.Content == "Título de prueba");
    }

    [Fact]
    public void GetLocalizationsForLanguage_ReturnsAllForLanguage()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var english = CreateTestLanguage("en-US");
        var spanish = CreateTestLanguage("es-ES");
        entity.AddLocalization("Title", "Test Title", english);
        entity.AddLocalization("Description", "Some description", english);
        entity.AddLocalization("Title", "Título de prueba", spanish);

        // Act
        var results = entity.GetLocalizationsForLanguage(english).ToList();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(l => l.FieldName == "Title" && l.Content == "Test Title");
        results.Should().Contain(l => l.FieldName == "Description");
    }

    [Fact]
    public void HasLocalization_ReturnsTrue_WhenExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Test Title", language);

        // Act
        var result = entity.HasLocalization("Title", language);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasLocalization_ReturnsFalse_WhenNotExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.HasLocalization("Title", language);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveLocalization_RemovesFromCollection()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Test Title", language);

        // Act
        var result = entity.RemoveLocalization("Title", language);

        // Assert
        result.Should().BeTrue();
        entity.Localizations.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLocalization_ReturnsFalse_WhenNotFound()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.RemoveLocalization("Title", language);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UpsertLocalization_AddsNew_WhenNotExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();

        // Act
        var result = entity.UpsertLocalization("Title", "Test Title", language);

        // Assert
        result.Should().NotBeNull();
        entity.Localizations.Should().ContainSingle();
        result.Content.Should().Be("Test Title");
    }

    [Fact]
    public void UpsertLocalization_UpdatesExisting_WhenExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Old Title", language);

        // Act
        var result = entity.UpsertLocalization("Title", "New Title", language);

        // Assert
        result.Should().NotBeNull();
        entity.Localizations.Should().ContainSingle();
        result.Content.Should().Be("New Title");
    }

    [Fact]
    public void UpsertLocalization_UpdatesStatus_WhenExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = CreateTestLanguage();
        entity.AddLocalization("Title", "Old Title", language, GameGuild.Localization.LocalizationStatus.Draft);

        // Act
        var result = entity.UpsertLocalization("Title", "New Title", language, 
            GameGuild.Localization.LocalizationStatus.Published);

        // Assert
        result.Status.Should().Be(GameGuild.Localization.LocalizationStatus.Published);
    }
}
