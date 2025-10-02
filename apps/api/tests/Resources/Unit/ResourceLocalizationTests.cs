using FluentAssertions;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Resources;
using Xunit;

namespace GameGuild.Tests.Resources.Unit;

/// <summary>
/// Unit tests for the ResourceLocalization entity
/// </summary>
public class ResourceLocalizationTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var resourceLocalization = new ResourceLocalization();

        // Assert
        resourceLocalization.Id.Should().NotBeEmpty();
        resourceLocalization.ResourceType.Should().BeEmpty();
        resourceLocalization.ResourceId.Should().Be(Guid.Empty);
        // LanguageId is handled by EF Core navigation property
        resourceLocalization.Language.Should().BeNull();
        resourceLocalization.FieldName.Should().BeEmpty();
        resourceLocalization.Content.Should().BeEmpty();
        resourceLocalization.IsDefault.Should().BeFalse();
        resourceLocalization.Status.Should().Be(LocalizationStatus.Draft);
        resourceLocalization.IsNew.Should().BeTrue();
        resourceLocalization.IsDeleted.Should().BeFalse();
        resourceLocalization.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        resourceLocalization.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_With_Object_Initializer_Should_Initialize_Correctly()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act
        var resourceLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = language,
            FieldName = "Title",
            Content = "Test Title"
        };

        // Assert
        resourceLocalization.ResourceType.Should().Be("Article");
        resourceLocalization.ResourceId.Should().Be(resourceId);
        resourceLocalization.Language.Should().Be(language);
        resourceLocalization.FieldName.Should().Be("Title");
        resourceLocalization.Content.Should().Be("Test Title");
        resourceLocalization.Status.Should().Be(LocalizationStatus.Draft);
    }

    [Theory]
    [InlineData("Article")]
    [InlineData("Course")]
    [InlineData("Media")]
    [InlineData("TestResource")]
    public void ResourceType_Property_Should_Accept_Valid_Resource_Types(string resourceType)
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act
        resourceLocalization.ResourceType = resourceType;

        // Assert
        resourceLocalization.ResourceType.Should().Be(resourceType);
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("Description")]
    [InlineData("Content")]
    [InlineData("Summary")]
    public void FieldName_Property_Should_Accept_Valid_Field_Names(string fieldName)
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act
        resourceLocalization.FieldName = fieldName;

        // Assert
        resourceLocalization.FieldName.Should().Be(fieldName);
    }

    [Fact]
    public void Content_Property_Should_Accept_Any_String_Value()
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();
        const string content = "This is a test content with special characters: áéíóú ñ ü";

        // Act
        resourceLocalization.Content = content;

        // Assert
        resourceLocalization.Content.Should().Be(content);
    }

    [Fact]
    public void Content_Property_Should_Accept_Empty_Or_Null_Values()
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act & Assert
        resourceLocalization.Content = "";
        resourceLocalization.Content.Should().Be("");

        resourceLocalization.Content = null!;
        resourceLocalization.Content.Should().BeNull();
    }

    [Theory]
    [InlineData(LocalizationStatus.Draft)]
    [InlineData(LocalizationStatus.Published)]
    [InlineData(LocalizationStatus.NeedsReview)]
    [InlineData(LocalizationStatus.Archived)]
    [InlineData(LocalizationStatus.MachineTranslated)]
    public void Status_Property_Should_Accept_All_LocalizationStatus_Values(LocalizationStatus status)
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act
        resourceLocalization.Status = status;

        // Assert
        resourceLocalization.Status.Should().Be(status);
    }

    [Fact]
    public void IsDefault_Property_Should_Be_Settable()
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act
        resourceLocalization.IsDefault = true;

        // Assert
        resourceLocalization.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Language_Navigation_Property_Should_Be_Settable()
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();
        var language = new Language { Code = "en-US", Name = "English (United States)" };

        // Act
        resourceLocalization.Language = language;

        // Assert
        resourceLocalization.Language.Should().Be(language);
    }

    [Fact]
    public void ResourceId_And_Language_Should_Be_Settable()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act
        var resourceLocalization = new ResourceLocalization
        {
            ResourceId = resourceId,
            Language = language
        };

        // Assert
        resourceLocalization.ResourceId.Should().Be(resourceId);
        resourceLocalization.Language.Should().Be(language);
    }

    [Fact]
    public void Should_Inherit_From_EntityBase()
    {
        // Act
        var resourceLocalization = new ResourceLocalization();

        // Assert
        resourceLocalization.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void Should_Have_Unique_Id_For_Each_Instance()
    {
        // Act
        var localization1 = new ResourceLocalization();
        var localization2 = new ResourceLocalization();

        // Assert
        localization1.Id.Should().NotBe(localization2.Id);
    }

    [Fact]
    public void Properties_Should_Support_Complex_Localization_Scenario()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var language = new Language { Code = "es-ES", Name = "Spanish (Spain)" };

        // Act
        var resourceLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = language,
            FieldName = "Title",
            Content = "Título del Artículo",
            IsDefault = false,
            Status = LocalizationStatus.Published
        };

        // Assert
        resourceLocalization.ResourceType.Should().Be("Article");
        resourceLocalization.ResourceId.Should().Be(resourceId);
        resourceLocalization.Language.Should().Be(language);
        resourceLocalization.FieldName.Should().Be("Title");
        resourceLocalization.Content.Should().Be("Título del Artículo");
        resourceLocalization.IsDefault.Should().BeFalse();
        resourceLocalization.Status.Should().Be(LocalizationStatus.Published);
    }

    [Fact]
    public void Should_Support_Multiple_Field_Localizations_For_Same_Resource()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var language = new Language { Code = "fr-FR", Name = "French (France)" };

        var titleLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = language,
            FieldName = "Title",
            Content = "Titre de l'Article",
            Status = LocalizationStatus.Published
        };

        var contentLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = language,
            FieldName = "Content",
            Content = "Contenu de l'article...",
            Status = LocalizationStatus.Draft
        };

        // Act & Assert
        titleLocalization.ResourceId.Should().Be(resourceId);
        contentLocalization.ResourceId.Should().Be(resourceId);
        titleLocalization.Language.Should().Be(language);
        contentLocalization.Language.Should().Be(language);
        titleLocalization.FieldName.Should().Be("Title");
        contentLocalization.FieldName.Should().Be("Content");
        titleLocalization.Status.Should().Be(LocalizationStatus.Published);
        contentLocalization.Status.Should().Be(LocalizationStatus.Draft);
    }

    [Fact]
    public void Should_Support_Default_Language_Marking()
    {
        // Arrange
        var resourceId = Guid.NewGuid();
        var englishLanguage = new Language { Code = "en-US", Name = "English", IsDefault = true };
        var spanishLanguage = new Language { Code = "es-ES", Name = "Spanish", IsDefault = false };

        var englishLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = englishLanguage,
            FieldName = "Title",
            Content = "English Title",
            IsDefault = true
        };

        var spanishLocalization = new ResourceLocalization
        {
            ResourceType = "Article",
            ResourceId = resourceId,
            Language = spanishLanguage,
            FieldName = "Title",
            Content = "Título en Español",
            IsDefault = false
        };

        // Act & Assert
        englishLocalization.IsDefault.Should().BeTrue();
        spanishLocalization.IsDefault.Should().BeFalse();
        englishLocalization.Language!.IsDefault.Should().BeTrue();
        spanishLocalization.Language!.IsDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData(null, null)]
    public void Should_Handle_Empty_Or_Null_String_Properties(string? fieldName, string? content)
    {
        // Arrange
        var resourceLocalization = new ResourceLocalization();

        // Act
        resourceLocalization.FieldName = fieldName!;
        resourceLocalization.Content = content!;

        // Assert
        resourceLocalization.FieldName.Should().Be(fieldName);
        resourceLocalization.Content.Should().Be(content);
    }
}