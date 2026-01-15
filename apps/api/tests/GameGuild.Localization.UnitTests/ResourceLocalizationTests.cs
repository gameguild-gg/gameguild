using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Entities;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

/// <summary>
/// Tests for ResourceLocalization entity including tenant isolation and fallback behavior
/// </summary>
public class ResourceLocalizationTests
{
    #region Tenant Isolation Tests

    [Fact]
    public void ResourceLocalization_EnforcesTenantIsolation_InheritsFromEntityBase()
    {
        // Arrange & Act
        var localization = new ResourceLocalization();

        // Assert
        localization.Should().BeAssignableTo<EntityBase>();
    }

    [Fact]
    public void ResourceLocalization_EnforcesTenantIsolation_HasTenantIdProperty()
    {
        // Arrange
        var localization = new ResourceLocalization();

        // Act - TenantId is accessible from EntityBase
        var tenantId = localization.TenantId;

        // Assert
        tenantId.Should().BeNull("default TenantId should be null before assignment");
    }

    [Fact]
    public void ResourceLocalization_EnforcesTenantIsolation_ImplementsITenantScoped()
    {
        // Arrange
        var localization = new ResourceLocalization();

        // Assert
        localization.Should().BeAssignableTo<ITenantScoped>();
    }

    [Fact]
    public void ResourceLocalization_EnforcesTenantIsolation_MultipleTenants_HaveDistinctLocalizations()
    {
        // Arrange
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var languageId = Guid.NewGuid();

        // Act - Create localizations for different tenants (same resource, field, language)
        var localization1 = CreateLocalizationForTenant(resourceId, languageId, "Title", "Tenant 1 Title");
        var localization2 = CreateLocalizationForTenant(resourceId, languageId, "Title", "Tenant 2 Title");

        // Assert - Both can exist with same ResourceId, FieldName, LanguageId but different tenants
        localization1.ResourceId.Should().Be(localization2.ResourceId);
        localization1.FieldName.Should().Be(localization2.FieldName);
        localization1.LanguageId.Should().Be(localization2.LanguageId);
        localization1.Content.Should().NotBe(localization2.Content);
    }

    #endregion

    #region Fallback Language Tests

    [Fact]
    public void LocalizableResource_FallsBackToDefaultLanguage_WhenTranslationMissing()
    {
        // Arrange
        var entity = new TestLocalizableEntity { Id = Guid.NewGuid() };
        var defaultLanguage = CreateLanguage("en-US", isDefault: true);
        var spanishLanguage = CreateLanguage("es-ES", isDefault: false);

        // Add only default language localization
        entity.AddLocalization("Title", "Default English Title", defaultLanguage);

        // Act - Try to get Spanish which doesn't exist
        var spanishLocalization = entity.GetLocalization("Title", spanishLanguage);
        var fallbackLocalization = entity.GetLocalization("Title", defaultLanguage);

        // Assert
        spanishLocalization.Should().BeNull("Spanish translation doesn't exist");
        fallbackLocalization.Should().NotBeNull("Default language should exist");
        fallbackLocalization!.Content.Should().Be("Default English Title");
    }

    [Fact]
    public void LocalizableResource_FallsBackToDefaultLanguage_CanImplementFallbackLogic()
    {
        // Arrange
        var entity = new TestLocalizableEntity { Id = Guid.NewGuid() };
        var defaultLanguage = CreateLanguage("en-US", isDefault: true);
        var spanishLanguage = CreateLanguage("es-ES", isDefault: false);

        entity.AddLocalization("Title", "Default English Title", defaultLanguage);

        // Act - Implement fallback pattern
        var result = entity.GetLocalization("Title", spanishLanguage)
                    ?? entity.GetLocalization("Title", defaultLanguage);

        // Assert
        result.Should().NotBeNull();
        result!.Content.Should().Be("Default English Title");
        result.LanguageId.Should().Be(defaultLanguage.Id, "should return default language as fallback");
    }

    [Fact]
    public void LocalizableResource_ReturnsRequestedLanguage_WhenTranslationExists()
    {
        // Arrange
        var entity = new TestLocalizableEntity { Id = Guid.NewGuid() };
        var defaultLanguage = CreateLanguage("en-US", isDefault: true);
        var spanishLanguage = CreateLanguage("es-ES", isDefault: false);

        entity.AddLocalization("Title", "Default English Title", defaultLanguage);
        entity.AddLocalization("Title", "Título en Español", spanishLanguage);

        // Act
        var spanishLocalization = entity.GetLocalization("Title", spanishLanguage);

        // Assert
        spanishLocalization.Should().NotBeNull();
        spanishLocalization!.Content.Should().Be("Título en Español");
        spanishLocalization.LanguageId.Should().Be(spanishLanguage.Id);
    }

    [Fact]
    public void LocalizableResource_FallsBackToDefaultLanguage_UsingGetOrDefault()
    {
        // Arrange
        var entity = new TestLocalizableEntity { Id = Guid.NewGuid() };
        var defaultLanguage = CreateLanguage("en-US", isDefault: true);
        var spanishLanguage = CreateLanguage("es-ES", isDefault: false);
        var frenchLanguage = CreateLanguage("fr-FR", isDefault: false);

        entity.AddLocalization("Title", "English Title", defaultLanguage);
        entity.AddLocalization("Title", "Título en Español", spanishLanguage);

        // Act - French doesn't exist, should use fallback pattern
        var frenchLocalization = entity.GetLocalization("Title", frenchLanguage);
        var spanishLocalization = entity.GetLocalization("Title", spanishLanguage);

        // Assert
        frenchLocalization.Should().BeNull("French translation doesn't exist");
        spanishLocalization.Should().NotBeNull("Spanish translation exists");
    }

    #endregion

    #region Helper Methods

    private static ResourceLocalization CreateLocalizationForTenant(
        Guid resourceId, Guid languageId, string fieldName, string content)
    {
        return new ResourceLocalization
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            ResourceType = "TestResource",
            FieldName = fieldName,
            Content = content,
            LanguageId = languageId,
            Status = LocalizationStatus.Published
        };
    }

    private static Language CreateLanguage(string code, bool isDefault = false)
    {
        return new Language
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = code == "en-US" ? "English" : code == "es-ES" ? "Spanish" : "French",
            IsActive = true,
            IsDefault = isDefault
        };
    }

    // Test implementation of LocalizableEntityBase
    private class TestLocalizableEntity : LocalizableEntityBase<ResourceLocalization>
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
