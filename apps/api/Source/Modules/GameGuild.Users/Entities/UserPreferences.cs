using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Users.Entities;

/// <summary>
///     User preferences entity for storing general, notification, accessibility, and privacy preferences
/// </summary>
[Table("UserPreferences")]
[Index(nameof(UserId), IsUnique = true)]
public class UserPreferences : EntityBase
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public UserPreferences() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial user preferences data</param>
    public UserPreferences(object partial) : base(partial) { }

    /// <summary>
    ///     ID of the user these preferences belong to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    ///     General application preferences (theme, language, timezone)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string GeneralPreferences { get; set; } = "{}";

    /// <summary>
    ///     Notification preferences (email, push, in-app)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string NotificationPreferences { get; set; } = "{}";

    /// <summary>
    ///     Accessibility preferences (font size, contrast, screen reader)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string AccessibilityPreferences { get; set; } = "{}";

    /// <summary>
    ///     Privacy preferences (profile visibility, data sharing)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string PrivacyPreferences { get; set; } = "{}";

    /// <summary>
    ///     Localization preferences (language, timezone, date/time formats)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string LocalizationPreferences { get; set; } = "{}";

    /// <summary>
    ///     Get general preferences as dictionary
    /// </summary>
    public Dictionary<string, object?> GetGeneralPreferences()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(GeneralPreferences) ?? new Dictionary<string, object?>(); }
        catch { return new Dictionary<string, object?>(); }
    }

    /// <summary>
    ///     Set general preferences from dictionary
    /// </summary>
    public void SetGeneralPreferences(Dictionary<string, object?> preferences)
    {
        GeneralPreferences = JsonSerializer.Serialize(preferences);
        Touch();
    }

    /// <summary>
    ///     Get notification preferences as dictionary
    /// </summary>
    public Dictionary<string, object?> GetNotificationPreferences()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(NotificationPreferences) ?? new Dictionary<string, object?>(); }
        catch { return new Dictionary<string, object?>(); }
    }

    /// <summary>
    ///     Set notification preferences from dictionary
    /// </summary>
    public void SetNotificationPreferences(Dictionary<string, object?> preferences)
    {
        NotificationPreferences = JsonSerializer.Serialize(preferences);
        Touch();
    }

    /// <summary>
    ///     Get accessibility preferences as dictionary
    /// </summary>
    public Dictionary<string, object?> GetAccessibilityPreferences()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(AccessibilityPreferences) ?? new Dictionary<string, object?>(); }
        catch { return new Dictionary<string, object?>(); }
    }

    /// <summary>
    ///     Set accessibility preferences from dictionary
    /// </summary>
    public void SetAccessibilityPreferences(Dictionary<string, object?> preferences)
    {
        AccessibilityPreferences = JsonSerializer.Serialize(preferences);
        Touch();
    }

    /// <summary>
    ///     Get privacy preferences as dictionary
    /// </summary>
    public Dictionary<string, object?> GetPrivacyPreferences()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(PrivacyPreferences) ?? new Dictionary<string, object?>(); }
        catch { return new Dictionary<string, object?>(); }
    }

    /// <summary>
    ///     Set privacy preferences from dictionary
    /// </summary>
    public void SetPrivacyPreferences(Dictionary<string, object?> preferences)
    {
        PrivacyPreferences = JsonSerializer.Serialize(preferences);
        Touch();
    }

    /// <summary>
    ///     Get localization preferences as dictionary
    /// </summary>
    public Dictionary<string, object?> GetLocalizationPreferences()
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object?>>(LocalizationPreferences) ?? new Dictionary<string, object?>(); }
        catch { return new Dictionary<string, object?>(); }
    }

    /// <summary>
    ///     Set localization preferences from dictionary
    /// </summary>
    public void SetLocalizationPreferences(Dictionary<string, object?> preferences)
    {
        LocalizationPreferences = JsonSerializer.Serialize(preferences);
        Touch();
    }

    /// <summary>
    ///     Reset all preferences to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        GeneralPreferences = "{}";
        NotificationPreferences = "{}";
        AccessibilityPreferences = "{}";
        PrivacyPreferences = "{}";
        LocalizationPreferences = "{}";
        Touch();
    }

    /// <summary>
    ///     Factory method to create user preferences with defaults
    /// </summary>
    public static UserPreferences Create(Guid userId) { return new UserPreferences { UserId = userId }; }
}
