using FluentAssertions;
using GameGuild.Modules.Localization;
using GameGuild.Modules.Resources;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Unit tests for the Language entity
/// </summary>
public class LanguageTests
{
    [Fact]
    public void Constructor_Should_Initialize_With_Default_Values()
    {
        // Act
        var language = new Language();

        // Assert
        language.Id.Should().NotBeEmpty();
        language.Code.Should().BeEmpty();
        language.Name.Should().BeEmpty();
        language.IsActive.Should().BeTrue();
        language.IsDefault.Should().BeFalse();
        language.ResourceLocalizations.Should().BeEmpty();
        language.IsNew.Should().BeTrue();
        language.IsDeleted.Should().BeFalse();
        language.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        language.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Constructor_With_Object_Initializer_Should_Initialize_Correctly()
    {
        // Act
        var language = new Language
        {
            Code = "en-US",
            Name = "English (United States)"
        };

        // Assert
        language.Code.Should().Be("en-US");
        language.Name.Should().Be("English (United States)");
        language.IsActive.Should().BeTrue();
        language.IsDefault.Should().BeFalse();
    }

    [Theory]
    [InlineData("en", "English")]
    [InlineData("es", "Spanish")]
    [InlineData("fr", "French")]
    [InlineData("de", "German")]
    public void Code_Property_Should_Accept_Valid_Language_Codes(string code, string name)
    {
        // Act
        var language = new Language
        {
            Code = code,
            Name = name
        };

        // Assert
        language.Code.Should().Be(code);
        language.Name.Should().Be(name);
    }

    [Fact]
    public void IsDefault_Property_Should_Be_Settable()
    {
        // Act
        var language = new Language
        {
            IsDefault = true
        };

        // Assert
        language.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void IsActive_Property_Should_Be_Settable()
    {
        // Act
        var language = new Language
        {
            IsActive = false
        };

        // Assert
        language.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ResourceLocalizations_Should_Be_Empty_Collection_By_Default()
    {
        // Act
        var language = new Language();

        // Assert
        language.ResourceLocalizations.Should().NotBeNull();
        language.ResourceLocalizations.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Code_Should_Accept_Empty_Or_Whitespace_Values(string code)
    {
        // Act
        var language = new Language
        {
            Code = code
        };

        // Assert
        language.Code.Should().Be(code);
    }

    [Fact]
    public void Should_Inherit_From_EntityBase()
    {
        // Act
        var language = new Language();

        // Assert
        language.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void Should_Have_Unique_Id_For_Each_Instance()
    {
        // Act
        var language1 = new Language();
        var language2 = new Language();

        // Assert
        language1.Id.Should().NotBe(language2.Id);
    }

    [Fact]
    public void Properties_Should_Be_Settable()
    {
        // Arrange
        const string code = "pt-BR";
        const string name = "Portuguese (Brazil)";

        // Act
        var language = new Language
        {
            Code = code,
            Name = name,
            IsActive = false,
            IsDefault = true
        };

        // Assert
        language.Code.Should().Be(code);
        language.Name.Should().Be(name);
        language.IsActive.Should().BeFalse();
        language.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void ResourceLocalizations_Collection_Should_Be_Modifiable()
    {
        // Arrange
        var language = new Language();
        var localization = new ResourceLocalization
        {
            ResourceType = "TestResource",
            Language = language,
            FieldName = "Name",
            Content = "Test Content"
        };

        // Act
        language.ResourceLocalizations.Add(localization);

        // Assert
        language.ResourceLocalizations.Should().HaveCount(1);
        language.ResourceLocalizations.Should().Contain(localization);
    }

    [Fact]
    public void Language_Should_Support_Multiple_Localizations()
    {
        // Arrange
        var language = new Language { Code = "en", Name = "English" };
        var localization1 = new ResourceLocalization
        {
            ResourceType = "Article",
            Language = language,
            FieldName = "Title",
            Content = "Article Title"
        };
        var localization2 = new ResourceLocalization
        {
            ResourceType = "Article",
            Language = language,
            FieldName = "Content",
            Content = "Article Content"
        };

        // Act
        language.ResourceLocalizations.Add(localization1);
        language.ResourceLocalizations.Add(localization2);

        // Assert
        language.ResourceLocalizations.Should().HaveCount(2);
        language.ResourceLocalizations.Should().Contain(localization1);
        language.ResourceLocalizations.Should().Contain(localization2);
    }
}