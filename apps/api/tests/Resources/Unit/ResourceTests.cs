using FluentAssertions;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using Xunit;

namespace GameGuild.Tests.Resources.Unit;

/// <summary>
/// Concrete implementation of Resource for testing purposes
/// </summary>
public class TestResource : Resource
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Unit tests for the Resource abstract base class
/// </summary>
public class ResourceTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var resource = new TestResource();

        // Assert
        resource.Id.Should().NotBeEmpty();
        resource.Localizations.Should().NotBeNull();
        resource.Localizations.Should().BeEmpty();
        resource.Tenant.Should().BeNull();
        resource.IsGlobal.Should().BeTrue(); // When Tenant is null
        resource.IsNew.Should().BeTrue();
        resource.IsDeleted.Should().BeFalse();
        resource.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        resource.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Should_Implement_ILocalizable_Interface()
    {
        // Act
        var resource = new TestResource();

        // Assert
        resource.Should().BeAssignableTo<ILocalizable>();
    }

    [Fact]
    public void Should_Implement_ITenantable_Interface()
    {
        // Act
        var resource = new TestResource();

        // Assert
        resource.Should().BeAssignableTo<ITenantable>();
    }

    [Fact]
    public void Should_Inherit_From_EntityBase()
    {
        // Act
        var resource = new TestResource();

        // Assert
        resource.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void IsGlobal_Should_Return_True_When_Tenant_Is_Null()
    {
        // Arrange
        var resource = new TestResource();

        // Act
        resource.Tenant = null;

        // Assert
        resource.IsGlobal.Should().BeTrue();
    }

    [Fact]
    public void IsGlobal_Should_Return_False_When_Tenant_Is_Set()
    {
        // Arrange
        var resource = new TestResource();
        var tenant = new Tenant { Name = "Test Tenant" };

        // Act
        resource.Tenant = tenant;

        // Assert
        resource.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void AssignToTenant_Should_Set_Tenant_Property()
    {
        // Arrange
        var resource = new TestResource();
        var tenant = new Tenant { Name = "Test Tenant" };

        // Act
        resource.AssignToTenant(tenant);

        // Assert
        resource.Tenant.Should().Be(tenant);
        resource.IsGlobal.Should().BeFalse();
    }

    [Fact]
    public void AddLocalization_Should_Create_And_Add_ResourceLocalization()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "en-US", Name = "English" };
        const string fieldName = "Title";
        const string content = "Test Title";

        // Act
        var localization = resource.AddLocalization(fieldName, content, language);

        // Assert
        localization.Should().NotBeNull();
        localization.ResourceType.Should().Be("TestResource");
        localization.Language.Should().Be(language);
        localization.FieldName.Should().Be(fieldName);
        localization.Content.Should().Be(content);
        localization.Status.Should().Be(LocalizationStatus.Draft); // Default status
        resource.Localizations.Should().Contain(localization);
        resource.Localizations.Should().HaveCount(1);
    }

    [Fact]
    public void AddLocalization_Should_Support_Custom_Status()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "es-ES", Name = "Spanish" };
        const string fieldName = "Description";
        const string content = "Descripción de prueba";
        const LocalizationStatus status = LocalizationStatus.Published;

        // Act
        var localization = resource.AddLocalization(fieldName, content, language, status);

        // Assert
        localization.Status.Should().Be(status);
        localization.ResourceType.Should().Be("TestResource");
        localization.Language.Should().Be(language);
        localization.FieldName.Should().Be(fieldName);
        localization.Content.Should().Be(content);
    }

    [Fact]
    public void AddLocalization_Should_Support_Multiple_Localizations()
    {
        // Arrange
        var resource = new TestResource();
        var englishLanguage = new Language { Code = "en-US", Name = "English" };
        var spanishLanguage = new Language { Code = "es-ES", Name = "Spanish" };

        // Act
        var titleEnglish = resource.AddLocalization("Title", "English Title", englishLanguage);
        var titleSpanish = resource.AddLocalization("Title", "Título en Español", spanishLanguage);
        var descriptionEnglish = resource.AddLocalization("Description", "English Description", englishLanguage);

        // Assert
        resource.Localizations.Should().HaveCount(3);
        resource.Localizations.Should().Contain(titleEnglish);
        resource.Localizations.Should().Contain(titleSpanish);
        resource.Localizations.Should().Contain(descriptionEnglish);
    }

    [Theory]
    [InlineData(LocalizationStatus.Draft)]
    [InlineData(LocalizationStatus.Published)]
    [InlineData(LocalizationStatus.NeedsReview)]
    [InlineData(LocalizationStatus.Archived)]
    [InlineData(LocalizationStatus.MachineTranslated)]
    public void AddLocalization_Should_Support_All_LocalizationStatus_Values(LocalizationStatus status)
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "fr-FR", Name = "French" };

        // Act
        var localization = resource.AddLocalization("Title", "Titre français", language, status);

        // Assert
        localization.Status.Should().Be(status);
    }

    [Fact]
    public void AddLocalization_Should_Return_Added_Localization()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "de-DE", Name = "German" };

        // Act
        var localization = resource.AddLocalization("Title", "Deutsche Titel", language);

        // Assert
        localization.Should().NotBeNull();
        localization.Should().BeOfType<ResourceLocalization>();
        resource.Localizations.Should().Contain(localization);
    }

    [Fact]
    public void Localizations_Collection_Should_Be_Modifiable()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "it-IT", Name = "Italian" };
        var manualLocalization = new ResourceLocalization
        {
            ResourceType = "TestResource",
            Language = language,
            FieldName = "Title",
            Content = "Titolo italiano"
        };

        // Act
        resource.Localizations.Add(manualLocalization);

        // Assert
        resource.Localizations.Should().HaveCount(1);
        resource.Localizations.Should().Contain(manualLocalization);
    }

    [Fact]
    public void Should_Support_Complex_Multi_Language_Scenario()
    {
        // Arrange
        var resource = new TestResource();
        var englishLanguage = new Language { Code = "en-US", Name = "English", IsDefault = true };
        var spanishLanguage = new Language { Code = "es-ES", Name = "Spanish" };
        var frenchLanguage = new Language { Code = "fr-FR", Name = "French" };

        // Act
        resource.AddLocalization("Title", "English Title", englishLanguage, LocalizationStatus.Published);
        resource.AddLocalization("Title", "Título en Español", spanishLanguage, LocalizationStatus.Published);
        resource.AddLocalization("Title", "Titre français", frenchLanguage, LocalizationStatus.Draft);
        resource.AddLocalization("Description", "English Description", englishLanguage, LocalizationStatus.Published);
        resource.AddLocalization("Description", "Descripción en español", spanishLanguage, LocalizationStatus.NeedsReview);

        // Assert
        resource.Localizations.Should().HaveCount(5);

        var englishTitleLocalizations = resource.Localizations.Where(l =>
            l.Language == englishLanguage && l.FieldName == "Title").ToList();
        var spanishLocalizations = resource.Localizations.Where(l =>
            l.Language == spanishLanguage).ToList();
        var titleLocalizations = resource.Localizations.Where(l =>
            l.FieldName == "Title").ToList();

        englishTitleLocalizations.Should().HaveCount(1);
        spanishLocalizations.Should().HaveCount(2);
        titleLocalizations.Should().HaveCount(3);
    }

    [Fact]
    public void Resource_Should_Maintain_ResourceType_Based_On_Class_Name()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act
        var localization = resource.AddLocalization("Title", "Test Title", language);

        // Assert
        localization.ResourceType.Should().Be("TestResource");
    }

    [Fact]
    public void Tenant_Assignment_Should_Change_IsGlobal_Property()
    {
        // Arrange
        var resource = new TestResource();
        var tenant = new Tenant { Name = "Corporate Tenant" };

        // Assert initial state
        resource.IsGlobal.Should().BeTrue();

        // Act
        resource.AssignToTenant(tenant);

        // Assert after assignment
        resource.IsGlobal.Should().BeFalse();
        resource.Tenant.Should().Be(tenant);
    }

    [Fact]
    public void Should_Have_Unique_Id_For_Each_Instance()
    {
        // Act
        var resource1 = new TestResource();
        var resource2 = new TestResource();

        // Assert
        resource1.Id.Should().NotBe(resource2.Id);
    }

    [Fact]
    public void AddLocalization_Should_Throw_For_Null_Language()
    {
        // Arrange
        var resource = new TestResource();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            resource.AddLocalization("Title", "Test Content", null!));
        exception.ParamName.Should().Be("language");
    }

    [Fact]
    public void AddLocalization_Should_Handle_Null_Content()
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            resource.AddLocalization("Title", null!, language));
        exception.ParamName.Should().Be("content");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AddLocalization_Should_Throw_For_Empty_Or_Whitespace_Content(string content)
    {
        // Arrange
        var resource = new TestResource();
        var language = new Language { Code = "en-US", Name = "English" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() =>
            resource.AddLocalization("Title", content, language));
        exception.ParamName.Should().Be("content");
    }
}