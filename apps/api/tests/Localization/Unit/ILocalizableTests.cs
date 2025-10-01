using FluentAssertions;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Resources;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Concrete test implementation of ILocalizable for testing purposes
/// </summary>
public class TestLocalizableEntity : EntityBase, ILocalizable
{
    public ICollection<ResourceLocalization> Localizations { get; set; } = [];

    public ResourceLocalization AddLocalization(string fieldName, string content, Language language, LocalizationStatus status = LocalizationStatus.Draft)
    {
        var localization = new ResourceLocalization
        {
            ResourceType = GetType().Name,
            Language = language,
            FieldName = fieldName,
            Content = content,
            Status = status
        };

        Localizations.Add(localization);
        return localization;
    }
}

/// <summary>
/// Unit tests for the ILocalizable interface
/// </summary>
public class ILocalizableTests
{
    [Fact]
    public void ILocalizable_Should_Define_Localizations_Property()
    {
        // Arrange
        var entity = new TestLocalizableEntity();

        // Act & Assert
        entity.Should().BeAssignableTo<ILocalizable>();
        entity.Localizations.Should().NotBeNull();
        entity.Localizations.Should().BeEmpty();
    }

    [Fact]
    public void ILocalizable_Should_Define_AddLocalization_Method()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act
        var localization = entity.AddLocalization("Title", "Test Title", language);

        // Assert
        localization.Should().NotBeNull();
        localization.Should().BeOfType<ResourceLocalization>();
        entity.Localizations.Should().Contain(localization);
    }

    [Fact]
    public void AddLocalization_Should_Support_Default_Status_Parameter()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "es-ES", Name = "Spanish" };

        // Act
        var localization = entity.AddLocalization("Description", "Test Description", language);

        // Assert
        localization.Status.Should().Be(LocalizationStatus.Draft);
    }

    [Fact]
    public void AddLocalization_Should_Support_Custom_Status_Parameter()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "fr-FR", Name = "French" };

        // Act
        var localization = entity.AddLocalization("Content", "Test Content", language, LocalizationStatus.Published);

        // Assert
        localization.Status.Should().Be(LocalizationStatus.Published);
    }

    [Fact]
    public void ILocalizable_Should_Support_Multiple_Implementations()
    {
        // Arrange
        var entity1 = new TestLocalizableEntity();
        var entity2 = new TestLocalizableEntity();
        var language = new Language { Code = "de-DE", Name = "German" };

        // Act
        entity1.AddLocalization("Title", "German Title 1", language);
        entity2.AddLocalization("Title", "German Title 2", language);

        // Assert
        entity1.Localizations.Should().HaveCount(1);
        entity2.Localizations.Should().HaveCount(1);
        entity1.Localizations.First().Content.Should().Be("German Title 1");
        entity2.Localizations.First().Content.Should().Be("German Title 2");
    }

    [Fact]
    public void Localizations_Collection_Should_Be_Modifiable()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "it-IT", Name = "Italian" };
        var manualLocalization = new ResourceLocalization
        {
            ResourceType = "TestLocalizableEntity",
            Language = language,
            FieldName = "Title",
            Content = "Manual Italian Title"
        };

        // Act
        entity.Localizations.Add(manualLocalization);

        // Assert
        entity.Localizations.Should().HaveCount(1);
        entity.Localizations.Should().Contain(manualLocalization);
    }

    [Theory]
    [InlineData(LocalizationStatus.Draft)]
    [InlineData(LocalizationStatus.Published)]
    [InlineData(LocalizationStatus.NeedsReview)]
    [InlineData(LocalizationStatus.Archived)]
    [InlineData(LocalizationStatus.MachineTranslated)]
    public void AddLocalization_Should_Support_All_Status_Values(LocalizationStatus status)
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "pt-BR", Name = "Portuguese (Brazil)" };

        // Act
        var localization = entity.AddLocalization("Summary", "Portuguese Summary", language, status);

        // Assert
        localization.Status.Should().Be(status);
    }

    [Fact]
    public void ILocalizable_Should_Allow_Multiple_Languages_For_Same_Field()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var englishLanguage = new Language { Code = "en-US", Name = "English" };
        var spanishLanguage = new Language { Code = "es-ES", Name = "Spanish" };
        var frenchLanguage = new Language { Code = "fr-FR", Name = "French" };

        // Act
        entity.AddLocalization("Title", "English Title", englishLanguage);
        entity.AddLocalization("Title", "Título Español", spanishLanguage);
        entity.AddLocalization("Title", "Titre Français", frenchLanguage);

        // Assert
        entity.Localizations.Should().HaveCount(3);
        var titleLocalizations = entity.Localizations.Where(l => l.FieldName == "Title").ToList();
        titleLocalizations.Should().HaveCount(3);
        titleLocalizations.Select(l => l.Language?.Code).Should().Contain(new[] { "en-US", "es-ES", "fr-FR" });
    }

    [Fact]
    public void ILocalizable_Should_Allow_Multiple_Fields_For_Same_Language()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "ja-JP", Name = "Japanese" };

        // Act
        entity.AddLocalization("Title", "日本語のタイトル", language);
        entity.AddLocalization("Description", "日本語の説明", language);
        entity.AddLocalization("Content", "日本語のコンテンツ", language);

        // Assert
        entity.Localizations.Should().HaveCount(3);
        var japaneseLocalizations = entity.Localizations.Where(l => l.Language == language).ToList();
        japaneseLocalizations.Should().HaveCount(3);
        japaneseLocalizations.Select(l => l.FieldName).Should().Contain(new[] { "Title", "Description", "Content" });
    }

    [Fact]
    public void AddLocalization_Return_Value_Should_Match_Added_Localization()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "ru-RU", Name = "Russian" };

        // Act
        var returnedLocalization = entity.AddLocalization("Title", "Русский заголовок", language, LocalizationStatus.Published);

        // Assert
        entity.Localizations.Should().Contain(returnedLocalization);
        entity.Localizations.First().Should().Be(returnedLocalization);
        returnedLocalization.ResourceType.Should().Be("TestLocalizableEntity");
        returnedLocalization.Language.Should().Be(language);
        returnedLocalization.FieldName.Should().Be("Title");
        returnedLocalization.Content.Should().Be("Русский заголовок");
        returnedLocalization.Status.Should().Be(LocalizationStatus.Published);
    }

    [Fact]
    public void ILocalizable_Implementation_Should_Be_Consistent()
    {
        // Arrange
        var entity = new TestLocalizableEntity();
        var language = new Language { Code = "ar-SA", Name = "Arabic" };

        // Act
        var localization1 = entity.AddLocalization("Title", "العنوان العربي", language);
        var localization2 = entity.AddLocalization("Description", "الوصف العربي", language);

        // Assert
        entity.Localizations.Should().HaveCount(2);
        localization1.ResourceType.Should().Be(localization2.ResourceType);
        localization1.Language.Should().Be(localization2.Language);
        localization1.ResourceType.Should().Be("TestLocalizableEntity");
    }
}