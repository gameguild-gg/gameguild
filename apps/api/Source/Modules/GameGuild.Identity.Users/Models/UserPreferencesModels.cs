namespace GameGuild.Identity.Users;

/// <summary>
///     Data transfer object for user preferences
/// </summary>
/// <param name="Id">The unique identifier for the user preferences</param>
/// <param name="UserId">The user identifier that these preferences belong to</param>
/// <param name="GeneralPreferences">General user preferences</param>
/// <param name="NotificationPreferences">Notification-specific preferences</param>
/// <param name="AccessibilityPreferences">Accessibility-specific preferences</param>
/// <param name="PrivacyPreferences">Privacy-specific preferences</param>
/// <param name="LocalizationPreferences">Localization-specific preferences</param>
/// <param name="CreatedAt">When the preferences were created</param>
/// <param name="UpdatedAt">When the preferences were last updated</param>
/// <param name="Version">Version for optimistic concurrency control</param>
public sealed record UserPreferencesDto(
    Guid Id,
    Guid UserId,
    Dictionary<string, object?> GeneralPreferences,
    Dictionary<string, object?> NotificationPreferences,
    Dictionary<string, object?> AccessibilityPreferences,
    Dictionary<string, object?> PrivacyPreferences,
    Dictionary<string, object?> LocalizationPreferences,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    byte[ ] Version
);

/// <summary>
///     Request model for updating user preferences
/// </summary>
/// <param name="GeneralPreferences">General preferences to update</param>
/// <param name="NotificationPreferences">Notification preferences to update</param>
/// <param name="AccessibilityPreferences">Accessibility preferences to update</param>
/// <param name="PrivacyPreferences">Privacy preferences to update</param>
public sealed record UpdateUserPreferencesRequest(
    Dictionary<string, object?>? GeneralPreferences = null,
    Dictionary<string, object?>? NotificationPreferences = null,
    Dictionary<string, object?>? AccessibilityPreferences = null,
    Dictionary<string, object?>? PrivacyPreferences = null
);

/// <summary>
///     Request model for completely replacing user preferences
/// </summary>
/// <param name="GeneralPreferences">Complete set of general preferences</param>
/// <param name="NotificationPreferences">Complete set of notification preferences</param>
/// <param name="AccessibilityPreferences">Complete set of accessibility preferences</param>
/// <param name="PrivacyPreferences">Complete set of privacy preferences</param>
public sealed record ReplaceUserPreferencesRequest(
    Dictionary<string, object?> GeneralPreferences,
    Dictionary<string, object?> NotificationPreferences,
    Dictionary<string, object?> AccessibilityPreferences,
    Dictionary<string, object?> PrivacyPreferences
);

/// <summary>
///     Data transfer object for user notification preferences
/// </summary>
/// <param name="EmailEnabled">Whether email notifications are enabled</param>
/// <param name="PushEnabled">Whether push notifications are enabled</param>
/// <param name="SmsEnabled">Whether SMS notifications are enabled</param>
/// <param name="InAppEnabled">Whether in-app notifications are enabled</param>
/// <param name="Frequency">Notification frequency preference</param>
/// <param name="QuietHours">Quiet hours configuration</param>
/// <param name="CategoryPreferences">Notification preferences by category</param>
public sealed record UserNotificationPreferencesDto(
    bool EmailEnabled,
    bool PushEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    string Frequency,
    Dictionary<string, object?> QuietHours,
    Dictionary<string, object?> CategoryPreferences
);

/// <summary>
///     Request model for updating user notification preferences
/// </summary>
/// <param name="NotificationPreferences">Notification preferences to update</param>
public sealed record UpdateUserNotificationPreferencesRequest(Dictionary<string, object?> NotificationPreferences);

/// <summary>
///     Request model for replacing user notification preferences
/// </summary>
/// <param name="NotificationPreferences">Complete set of notification preferences</param>
public sealed record ReplaceUserNotificationPreferencesRequest(Dictionary<string, object?> NotificationPreferences);

/// <summary>
///     Data transfer object for user accessibility preferences
/// </summary>
/// <param name="HighContrast">Whether high contrast mode is enabled</param>
/// <param name="LargeText">Whether large text mode is enabled</param>
/// <param name="ScreenReader">Whether screen reader support is enabled</param>
/// <param name="ReducedMotion">Whether reduced motion is preferred</param>
/// <param name="KeyboardNavigation">Whether keyboard navigation is preferred</param>
/// <param name="FontSize">Preferred font size</param>
/// <param name="ColorScheme">Preferred color scheme</param>
/// <param name="CustomSettings">Custom accessibility settings</param>
public sealed record UserAccessibilityPreferencesDto(
    bool HighContrast,
    bool LargeText,
    bool ScreenReader,
    bool ReducedMotion,
    bool KeyboardNavigation,
    int FontSize,
    string ColorScheme,
    Dictionary<string, object?> CustomSettings
);

/// <summary>
///     Request model for updating user accessibility preferences
/// </summary>
/// <param name="AccessibilityPreferences">Accessibility preferences to update</param>
public sealed record UpdateUserAccessibilityPreferencesRequest(Dictionary<string, object?> AccessibilityPreferences);

/// <summary>
///     Request model for replacing user accessibility preferences
/// </summary>
/// <param name="AccessibilityPreferences">Complete set of accessibility preferences</param>
public sealed record ReplaceUserAccessibilityPreferencesRequest(Dictionary<string, object?> AccessibilityPreferences);

/// <summary>
///     Data transfer object for user privacy preferences
/// </summary>
/// <param name="ProfileVisibility">Profile visibility setting</param>
/// <param name="ActivityTracking">Whether activity tracking is allowed</param>
/// <param name="DataCollection">Data collection preferences</param>
/// <param name="ThirdPartySharing">Third-party sharing preferences</param>
/// <param name="MarketingEmails">Whether marketing emails are allowed</param>
/// <param name="AnalyticsCookies">Whether analytics cookies are allowed</param>
/// <param name="PersonalizedContent">Whether personalized content is allowed</param>
/// <param name="CustomSettings">Custom privacy settings</param>
public sealed record UserPrivacyPreferencesDto(
    string ProfileVisibility,
    bool ActivityTracking,
    Dictionary<string, object?> DataCollection,
    Dictionary<string, object?> ThirdPartySharing,
    bool MarketingEmails,
    bool AnalyticsCookies,
    bool PersonalizedContent,
    Dictionary<string, object?> CustomSettings
);

/// <summary>
///     Request model for updating user privacy preferences
/// </summary>
/// <param name="PrivacyPreferences">Privacy preferences to update</param>
public sealed record UpdateUserPrivacyPreferencesRequest(Dictionary<string, object?> PrivacyPreferences);

/// <summary>
///     Request model for replacing user privacy preferences
/// </summary>
/// <param name="PrivacyPreferences">Complete set of privacy preferences</param>
public sealed record ReplaceUserPrivacyPreferencesRequest(Dictionary<string, object?> PrivacyPreferences);

/// <summary>
///     Data transfer object for user localization preferences
/// </summary>
/// <param name="Language">Preferred language code (e.g., en-US, es-ES)</param>
/// <param name="Timezone">Preferred timezone (e.g., UTC, America/New_York)</param>
/// <param name="DateFormat">Preferred date format (e.g., MM/dd/yyyy, dd/MM/yyyy)</param>
/// <param name="TimeFormat">Preferred time format (12h or 24h)</param>
/// <param name="Currency">Preferred currency code (e.g., USD, EUR)</param>
/// <param name="NumberFormat">Number formatting preferences</param>
/// <param name="CustomSettings">Custom localization settings</param>
public sealed record UserLocalizationPreferencesDto(string Language, string Timezone, string DateFormat, string TimeFormat, string Currency, Dictionary<string, object?> NumberFormat, Dictionary<string, object?> CustomSettings);

/// <summary>
///     Request model for updating user localization preferences
/// </summary>
/// <param name="LocalizationPreferences">Localization preferences to update</param>
public sealed record UpdateUserLocalizationPreferencesRequest(Dictionary<string, object?> LocalizationPreferences);

/// <summary>
///     Request model for replacing user localization preferences
/// </summary>
/// <param name="LocalizationPreferences">Complete set of localization preferences</param>
public sealed record ReplaceUserLocalizationPreferencesRequest(Dictionary<string, object?> LocalizationPreferences);
