using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.Entities;

public class UserPreferencesCoverageTests
{
    [Fact]
    public void GetPreferenceMethods_WithInvalidJson_ShouldReturnEmptyDictionaries()
    {
        var preferences = new UserPreferences
        {
            GeneralPreferences = "{",
            NotificationPreferences = "{",
            AccessibilityPreferences = "{",
            PrivacyPreferences = "{",
            LocalizationPreferences = "{"
        };

        preferences.GetGeneralPreferences().Should().BeEmpty();
        preferences.GetNotificationPreferences().Should().BeEmpty();
        preferences.GetAccessibilityPreferences().Should().BeEmpty();
        preferences.GetPrivacyPreferences().Should().BeEmpty();
        preferences.GetLocalizationPreferences().Should().BeEmpty();
    }

    [Fact]
    public void GetPreferenceMethods_WhenJsonDeserializesToNull_ShouldReturnEmptyDictionaries()
    {
        var preferences = new UserPreferences
        {
            GeneralPreferences = "null",
            NotificationPreferences = "null",
            AccessibilityPreferences = "null",
            PrivacyPreferences = "null",
            LocalizationPreferences = "null"
        };

        preferences.GetGeneralPreferences().Should().BeEmpty();
        preferences.GetNotificationPreferences().Should().BeEmpty();
        preferences.GetAccessibilityPreferences().Should().BeEmpty();
        preferences.GetPrivacyPreferences().Should().BeEmpty();
        preferences.GetLocalizationPreferences().Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithPartialObject_ShouldMapProvidedValuesAndGenerateId()
    {
        var userId = Guid.NewGuid();
        var preferences = new UserPreferences(new
        {
            UserId = userId,
            GeneralPreferences = "{\"theme\":\"dark\"}"
        });

        preferences.Id.Should().NotBe(Guid.Empty);
        preferences.UserId.Should().Be(userId);
        preferences.GeneralPreferences.Should().Be("{\"theme\":\"dark\"}");
    }

    [Fact]
    public void SettersAndResetToDefaults_ShouldRoundTripAllPreferenceCategories()
    {
        var userId = Guid.NewGuid();
        var preferences = UserPreferences.Create(userId);

        preferences.SetGeneralPreferences(new Dictionary<string, object?> { ["theme"] = "dark" });
        preferences.SetNotificationPreferences(new Dictionary<string, object?> { ["email"] = true });
        preferences.SetAccessibilityPreferences(new Dictionary<string, object?> { ["fontSize"] = 18 });
        preferences.SetPrivacyPreferences(new Dictionary<string, object?> { ["shareData"] = false });
        preferences.SetLocalizationPreferences(new Dictionary<string, object?> { ["language"] = "en-US" });

        ((System.Text.Json.JsonElement)preferences.GetGeneralPreferences()["theme"]!).GetString().Should().Be("dark");
        ((System.Text.Json.JsonElement)preferences.GetNotificationPreferences()["email"]!).GetBoolean().Should().BeTrue();
        ((System.Text.Json.JsonElement)preferences.GetAccessibilityPreferences()["fontSize"]!).GetInt32().Should().Be(18);
        ((System.Text.Json.JsonElement)preferences.GetPrivacyPreferences()["shareData"]!).GetBoolean().Should().BeFalse();
        ((System.Text.Json.JsonElement)preferences.GetLocalizationPreferences()["language"]!).GetString().Should().Be("en-US");

        preferences.ResetToDefaults();

        preferences.UserId.Should().Be(userId);
        preferences.GeneralPreferences.Should().Be("{}");
        preferences.NotificationPreferences.Should().Be("{}");
        preferences.AccessibilityPreferences.Should().Be("{}");
        preferences.PrivacyPreferences.Should().Be("{}");
        preferences.LocalizationPreferences.Should().Be("{}");
    }
}
