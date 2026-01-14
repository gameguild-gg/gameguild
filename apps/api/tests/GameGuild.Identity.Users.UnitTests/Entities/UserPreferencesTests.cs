using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserPreferencesTests
{
    [Fact]
    public void Create_ShouldInitializeWithUserId()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var preferences = UserPreferences.Create(userId);

        // Assert
        preferences.Should().NotBeNull();
        preferences.UserId.Should().Be(userId);
        preferences.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void SetGeneralPreferences_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        var prefs = new Dictionary<string, object?>
        {
            ["theme"] = "dark",
            ["language"] = "en",
            ["timezone"] = "UTC"
        };

        // Act
        preferences.SetGeneralPreferences(prefs);
        var retrieved = preferences.GetGeneralPreferences();

        // Assert
        retrieved.Should().ContainKey("theme");
        retrieved.Should().ContainKey("language");
        retrieved.Should().ContainKey("timezone");
    }

    [Fact]
    public void SetNotificationPreferences_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        var prefs = new Dictionary<string, object?>
        {
            ["emailEnabled"] = true,
            ["pushEnabled"] = false
        };

        // Act
        preferences.SetNotificationPreferences(prefs);
        var retrieved = preferences.GetNotificationPreferences();

        // Assert
        retrieved.Should().ContainKey("emailEnabled");
        retrieved.Should().ContainKey("pushEnabled");
    }

    [Fact]
    public void SetAccessibilityPreferences_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        var prefs = new Dictionary<string, object?>
        {
            ["fontSize"] = "large",
            ["highContrast"] = true
        };

        // Act
        preferences.SetAccessibilityPreferences(prefs);
        var retrieved = preferences.GetAccessibilityPreferences();

        // Assert
        retrieved.Should().ContainKey("fontSize");
        retrieved.Should().ContainKey("highContrast");
    }

    [Fact]
    public void SetPrivacyPreferences_ShouldStoreAndRetrieveCorrectly()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        var prefs = new Dictionary<string, object?>
        {
            ["profileVisible"] = false,
            ["shareData"] = true
        };

        // Act
        preferences.SetPrivacyPreferences(prefs);
        var retrieved = preferences.GetPrivacyPreferences();

        // Assert
        retrieved.Should().ContainKey("profileVisible");
        retrieved.Should().ContainKey("shareData");
    }

    [Fact]
    public void ResetToDefaults_ShouldClearAllPreferences()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["test"] = "value" });

        // Act
        preferences.ResetToDefaults();
        var retrieved = preferences.GetGeneralPreferences();

        // Assert
        retrieved.Should().BeEmpty();
    }

    [Fact]
    public void GetGeneralPreferences_WithInvalidJson_ShouldReturnEmptyDictionary()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.GeneralPreferences = "invalid json";

        // Act
        var result = preferences.GetGeneralPreferences();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SetGeneralPreferences_ShouldSerializeAndStore()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        var prefs = new Dictionary<string, object?> 
        { 
            ["theme"] = "dark",
            ["language"] = "en"
        };

        // Act
        preferences.SetGeneralPreferences(prefs);
        var retrieved = preferences.GetGeneralPreferences();

        // Assert
        retrieved.Should().ContainKey("theme");
        retrieved.Should().ContainKey("language");
    }

    [Fact]
    public void ResetToDefaults_ShouldResetAllPreferencesToEmpty()
    {
        // Arrange
        var preferences = UserPreferences.Create(Guid.NewGuid());
        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["theme"] = "dark" });
        preferences.SetNotificationPreferences(new Dictionary<string, object?> { ["email"] = true });
        preferences.SetAccessibilityPreferences(new Dictionary<string, object?> { ["fontSize"] = "large" });
        preferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["visible"] = false });

        // Act
        preferences.ResetToDefaults();

        // Assert
        preferences.GetGeneralPreferences().Should().BeEmpty();
        preferences.GetNotificationPreferences().Should().BeEmpty();
        preferences.GetAccessibilityPreferences().Should().BeEmpty();
        preferences.GetPrivacyPreferences().Should().BeEmpty();
    }
}
