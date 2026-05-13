using FluentAssertions;

using static GameGuild.Identity.Users.UnitTests.JsonTestData;
using Xunit;

namespace GameGuild.Identity.Users.Tests;

/// <summary>
///     Coverage boost tests for UserPreferences entity and all preferences-related DTOs.
/// </summary>
public class UserPreferencesModelAndEntityTests
{
    // ── UserPreferences entity tests ──

    [Fact]
    public void Create_ShouldReturnNewInstance()
    {
        var userId = Guid.NewGuid();
        var prefs = UserPreferences.Create(userId);

        prefs.UserId.Should().Be(userId);
    }

    [Fact]
    public void GetSetGeneralPreferences_ShouldRoundTrip()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        var data = new Dictionary<string, object?> { ["theme"] = "dark" };

        prefs.SetGeneralPreferences(data);
        var result = prefs.GetGeneralPreferences();

        result.Should().ContainKey("theme");
    }

    [Fact]
    public void GetSetNotificationPreferences_ShouldRoundTrip()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        var data = new Dictionary<string, object?> { ["email"] = true };

        prefs.SetNotificationPreferences(data);
        var result = prefs.GetNotificationPreferences();

        result.Should().ContainKey("email");
    }

    [Fact]
    public void GetSetAccessibilityPreferences_ShouldRoundTrip()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        var data = new Dictionary<string, object?> { ["highContrast"] = true };

        prefs.SetAccessibilityPreferences(data);
        var result = prefs.GetAccessibilityPreferences();

        result.Should().ContainKey("highContrast");
    }

    [Fact]
    public void GetSetPrivacyPreferences_ShouldRoundTrip()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        var data = new Dictionary<string, object?> { ["tracking"] = false };

        prefs.SetPrivacyPreferences(data);
        var result = prefs.GetPrivacyPreferences();

        result.Should().ContainKey("tracking");
    }

    [Fact]
    public void GetSetLocalizationPreferences_ShouldRoundTrip()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        var data = new Dictionary<string, object?> { ["language"] = "en" };

        prefs.SetLocalizationPreferences(data);
        var result = prefs.GetLocalizationPreferences();

        result.Should().ContainKey("language");
    }

    [Fact]
    public void ResetToDefaults_ShouldClearAllPreferences()
    {
        var prefs = UserPreferences.Create(Guid.NewGuid());
        prefs.SetGeneralPreferences(new Dictionary<string, object?> { ["key"] = "val" });
        prefs.SetNotificationPreferences(new Dictionary<string, object?> { ["k"] = "v" });

        prefs.ResetToDefaults();

        prefs.GetGeneralPreferences().Should().BeEmpty();
        prefs.GetNotificationPreferences().Should().BeEmpty();
    }

    // ── DTO instantiation tests ──

    [Fact]
    public void UserPreferencesDto_ShouldInstantiate()
    {
        var empty = JsonMap(new Dictionary<string, object?>());
        var dto = new UserPreferencesDto(
            Guid.NewGuid(), Guid.NewGuid(),
            empty, empty, empty, empty, empty,
            DateTimeOffset.UtcNow, null, Array.Empty<byte>());

        dto.GeneralPreferences.Should().NotBeNull();
    }

    [Fact]
    public void UpdateUserPreferencesRequest_ShouldInstantiate()
    {
        var req = new UpdateUserPreferencesRequest(
            GeneralPreferences: JsonMap(new Dictionary<string, object?> { ["x"] = 1 }),
            NotificationPreferences: null,
            AccessibilityPreferences: null,
            PrivacyPreferences: null);

        req.GeneralPreferences.Should().ContainKey("x");
    }

    [Fact]
    public void ReplaceUserPreferencesRequest_ShouldInstantiate()
    {
        var d = JsonMap(new Dictionary<string, object?>());
        var req = new ReplaceUserPreferencesRequest(d, d, d, d);

        req.GeneralPreferences.Should().NotBeNull();
    }

    [Fact]
    public void UserNotificationPreferencesDto_ShouldInstantiate()
    {
        var dto = new UserNotificationPreferencesDto(
            true, false, false, true, "Daily",
            JsonMap(new Dictionary<string, object?>()),
            JsonMap(new Dictionary<string, object?>()));

        dto.EmailEnabled.Should().BeTrue();
        dto.Frequency.Should().Be("Daily");
    }

    [Fact]
    public void UpdateAndReplaceNotificationPreferencesRequest_ShouldInstantiate()
    {
        var d = JsonMap(new Dictionary<string, object?> { ["k"] = "v" });
        var update = new UpdateUserNotificationPreferencesRequest(d);
        var replace = new ReplaceUserNotificationPreferencesRequest(d);

        update.NotificationPreferences.Should().ContainKey("k");
        replace.NotificationPreferences.Should().ContainKey("k");
    }

    [Fact]
    public void UserAccessibilityPreferencesDto_ShouldInstantiate()
    {
        var dto = new UserAccessibilityPreferencesDto(
            true, false, true, false, true, 16, "dark",
            JsonMap(new Dictionary<string, object?>()));

        dto.HighContrast.Should().BeTrue();
        dto.FontSize.Should().Be(16);
    }

    [Fact]
    public void UpdateAndReplaceAccessibilityPreferencesRequest_ShouldInstantiate()
    {
        var d = JsonMap(new Dictionary<string, object?>());
        var update = new UpdateUserAccessibilityPreferencesRequest(d);
        var replace = new ReplaceUserAccessibilityPreferencesRequest(d);

        update.AccessibilityPreferences.Should().NotBeNull();
        replace.AccessibilityPreferences.Should().NotBeNull();
    }

    [Fact]
    public void UserPrivacyPreferencesDto_ShouldInstantiate()
    {
        var dto = new UserPrivacyPreferencesDto(
            "Public", true,
            JsonMap(new Dictionary<string, object?>()),
            JsonMap(new Dictionary<string, object?>()),
            false, true, false,
            JsonMap(new Dictionary<string, object?>()));

        dto.ProfileVisibility.Should().Be("Public");
        dto.ActivityTracking.Should().BeTrue();
    }

    [Fact]
    public void UpdateAndReplacePrivacyPreferencesRequest_ShouldInstantiate()
    {
        var d = JsonMap(new Dictionary<string, object?>());
        var update = new UpdateUserPrivacyPreferencesRequest(d);
        var replace = new ReplaceUserPrivacyPreferencesRequest(d);

        update.PrivacyPreferences.Should().NotBeNull();
        replace.PrivacyPreferences.Should().NotBeNull();
    }

    [Fact]
    public void UserLocalizationPreferencesDto_ShouldInstantiate()
    {
        var dto = new UserLocalizationPreferencesDto(
            "en", "UTC", "MM/dd/yyyy", "HH:mm", "USD",
            JsonMap(new Dictionary<string, object?>()),
            JsonMap(new Dictionary<string, object?>()));

        dto.Language.Should().Be("en");
        dto.Currency.Should().Be("USD");
    }

    [Fact]
    public void UpdateAndReplaceLocalizationPreferencesRequest_ShouldInstantiate()
    {
        var d = JsonMap(new Dictionary<string, object?>());
        var update = new UpdateUserLocalizationPreferencesRequest(d);
        var replace = new ReplaceUserLocalizationPreferencesRequest(d);

        update.LocalizationPreferences.Should().NotBeNull();
        replace.LocalizationPreferences.Should().NotBeNull();
    }
}
