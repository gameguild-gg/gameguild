using FluentAssertions;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Models;

/// <summary>
/// Unit tests for ResourceLocalization entity
/// </summary>
public class ResourceLocalizationEntityTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrectlySet()
    {
        // Arrange & Act
        var localization = new ResourceLocalization();

        // Assert
        localization.ResourceType.Should().BeEmpty();
        localization.FieldName.Should().BeEmpty();
        localization.Content.Should().BeEmpty();
        localization.Status.Should().Be(LocalizationStatus.Draft);
    }

    [Fact]
    public void SetProperties_ShouldUpdateValues()
    {
        // Arrange
        var localization = new ResourceLocalization();
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();

        // Act
        localization.ResourceId = resourceId;
        localization.ResourceType = "Course";
        localization.FieldName = "Title";
        localization.Content = "Curso de Programación";
        localization.LanguageId = languageId;
        localization.Status = LocalizationStatus.Published;

        // Assert
        localization.ResourceId.Should().Be(resourceId);
        localization.ResourceType.Should().Be("Course");
        localization.FieldName.Should().Be("Title");
        localization.Content.Should().Be("Curso de Programación");
        localization.LanguageId.Should().Be(languageId);
        localization.Status.Should().Be(LocalizationStatus.Published);
    }

    [Theory]
    [InlineData("Title")]
    [InlineData("Description")]
    [InlineData("ShortDescription")]
    [InlineData("AltText")]
    public void FieldName_ShouldAcceptCommonFieldNames(string fieldName)
    {
        // Arrange
        var localization = new ResourceLocalization();

        // Act
        localization.FieldName = fieldName;

        // Assert
        localization.FieldName.Should().Be(fieldName);
    }

    [Theory]
    [InlineData("Course")]
    [InlineData("Project")]
    [InlineData("AssetReference")]
    [InlineData("Post")]
    public void ResourceType_ShouldAcceptCommonResourceTypes(string resourceType)
    {
        // Arrange
        var localization = new ResourceLocalization();

        // Act
        localization.ResourceType = resourceType;

        // Assert
        localization.ResourceType.Should().Be(resourceType);
    }
}

/// <summary>
/// Unit tests for LocalizationStatus enum
/// </summary>
public class LocalizationStatusEnumTests
{
    [Theory]
    [InlineData(LocalizationStatus.Draft)]
    [InlineData(LocalizationStatus.Published)]
    [InlineData(LocalizationStatus.NeedsReview)]
    [InlineData(LocalizationStatus.Archived)]
    [InlineData(LocalizationStatus.MachineTranslated)]
    public void LocalizationStatus_ShouldHaveExpectedValues(LocalizationStatus status)
    {
        // Assert
        Enum.IsDefined(typeof(LocalizationStatus), status).Should().BeTrue();
    }

    [Fact]
    public void LocalizationStatus_ShouldHaveCorrectCount()
    {
        // Assert
        Enum.GetValues<LocalizationStatus>().Should().HaveCountGreaterOrEqualTo(4);
    }
}
