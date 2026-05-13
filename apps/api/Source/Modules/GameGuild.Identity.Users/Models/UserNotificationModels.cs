using System.Text.Json;

namespace GameGuild.Identity.Users;

/// <summary>
///     Data transfer object for user notification
/// </summary>
/// <param name="Id">The unique identifier for the notification</param>
/// <param name="UserId">The user identifier that this notification belongs to</param>
/// <param name="Type">Type of notification</param>
/// <param name="Title">Notification title</param>
/// <param name="Message">Notification message content</param>
/// <param name="Priority">Notification priority level</param>
/// <param name="Category">Notification category</param>
/// <param name="IsRead">Whether the notification has been read</param>
/// <param name="IsArchived">Whether the notification is archived</param>
/// <param name="ReadAt">When the notification was read</param>
/// <param name="ArchivedAt">When the notification was archived</param>
/// <param name="ExpiresAt">When the notification expires</param>
/// <param name="ActionUrl">URL for notification action</param>
/// <param name="ActionText">Text for notification action</param>
/// <param name="ImageUrl">URL to notification image</param>
/// <param name="Metadata">Additional notification metadata</param>
/// <param name="CreatedAt">When the notification was created</param>
/// <param name="UpdatedAt">When the notification was last updated</param>
/// <param name="Version">Version for optimistic concurrency control</param>
public sealed record UserNotificationDto(
    Guid Id,
    Guid UserId,
    string Type,
    string Title,
    string Message,
    string Priority,
    string? Category,
    bool IsRead,
    bool IsArchived,
    DateTimeOffset? ReadAt,
    DateTimeOffset? ArchivedAt,
    DateTimeOffset? ExpiresAt,
    string? ActionUrl,
    string? ActionText,
    string? ImageUrl,
    Dictionary<string, JsonElement> Metadata,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    byte[ ] Version
);

/// <summary>
///     Data transfer object for notification counts
/// </summary>
/// <param name="Total">Total number of notifications</param>
/// <param name="Unread">Number of unread notifications</param>
/// <param name="Archived">Number of archived notifications</param>
/// <param name="ByPriority">Count of notifications by priority level</param>
/// <param name="ByCategory">Count of notifications by category</param>
public sealed record UserNotificationCountDto(int Total, int Unread, int Archived, Dictionary<string, int> ByPriority, Dictionary<string, int> ByCategory);

/// <summary>
///     Data transfer object for detailed notification information
/// </summary>
/// <param name="Notification">The notification details</param>
/// <param name="RelatedNotifications">Related notifications</param>
/// <param name="Actions">Available actions for the notification</param>
public sealed record UserNotificationDetailDto(UserNotificationDto Notification, List<UserNotificationDto> RelatedNotifications, List<NotificationActionDto> Actions);

/// <summary>
///     Data transfer object for notification action
/// </summary>
/// <param name="Id">Action identifier</param>
/// <param name="Text">Action display text</param>
/// <param name="Url">Action URL</param>
/// <param name="Type">Action type</param>
/// <param name="IsPrimary">Whether this is the primary action</param>
public sealed record NotificationActionDto(string Id, string Text, string? Url, string Type, bool IsPrimary);

/// <summary>
///     Request model for executing a notification action
/// </summary>
/// <param name="ActionId">The identifier of the action to execute</param>
/// <param name="Parameters">Additional parameters for the action</param>
public sealed record ExecuteNotificationActionRequest(string ActionId, Dictionary<string, JsonElement>? Parameters = null);

/// <summary>
///     Data transfer object for notification action result
/// </summary>
/// <param name="Success">Whether the action was successful</param>
/// <param name="Message">Result message</param>
/// <param name="RedirectUrl">URL to redirect to after action</param>
/// <param name="UpdatedNotification">Updated notification after action</param>
public sealed record NotificationActionResultDto(bool Success, string? Message, string? RedirectUrl, UserNotificationDto? UpdatedNotification);

/// <summary>
///     Data transfer object for notification delivery settings
/// </summary>
/// <param name="UserId">User identifier</param>
/// <param name="EmailEnabled">Whether email delivery is enabled</param>
/// <param name="PushEnabled">Whether push delivery is enabled</param>
/// <param name="SmsEnabled">Whether SMS delivery is enabled</param>
/// <param name="InAppEnabled">Whether in-app delivery is enabled</param>
/// <param name="EmailFrequency">Email delivery frequency</param>
/// <param name="PushFrequency">Push delivery frequency</param>
/// <param name="QuietHoursStart">Start of quiet hours</param>
/// <param name="QuietHoursEnd">End of quiet hours</param>
/// <param name="TimeZone">User's timezone for delivery scheduling</param>
/// <param name="CategorySettings">Per-category delivery settings</param>
public sealed record UserNotificationDeliverySettingsDto(
    Guid UserId,
    bool EmailEnabled,
    bool PushEnabled,
    bool SmsEnabled,
    bool InAppEnabled,
    string EmailFrequency,
    string PushFrequency,
    TimeOnly? QuietHoursStart,
    TimeOnly? QuietHoursEnd,
    string TimeZone,
    Dictionary<string, NotificationCategorySettingsDto> CategorySettings
);

/// <summary>
///     Data transfer object for notification category settings
/// </summary>
/// <param name="Enabled">Whether notifications for this category are enabled</param>
/// <param name="EmailEnabled">Whether email delivery is enabled for this category</param>
/// <param name="PushEnabled">Whether push delivery is enabled for this category</param>
/// <param name="SmsEnabled">Whether SMS delivery is enabled for this category</param>
/// <param name="Priority">Minimum priority for notifications in this category</param>
public sealed record NotificationCategorySettingsDto(bool Enabled, bool EmailEnabled, bool PushEnabled, bool SmsEnabled, string Priority);

/// <summary>
///     Request model for updating notification delivery settings
/// </summary>
/// <param name="EmailEnabled">Whether email delivery is enabled</param>
/// <param name="PushEnabled">Whether push delivery is enabled</param>
/// <param name="SmsEnabled">Whether SMS delivery is enabled</param>
/// <param name="InAppEnabled">Whether in-app delivery is enabled</param>
/// <param name="EmailFrequency">Email delivery frequency</param>
/// <param name="PushFrequency">Push delivery frequency</param>
/// <param name="QuietHoursStart">Start of quiet hours</param>
/// <param name="QuietHoursEnd">End of quiet hours</param>
/// <param name="TimeZone">User's timezone</param>
/// <param name="CategorySettings">Per-category delivery settings</param>
public sealed record UpdateUserNotificationDeliverySettingsRequest(
    bool? EmailEnabled = null,
    bool? PushEnabled = null,
    bool? SmsEnabled = null,
    bool? InAppEnabled = null,
    string? EmailFrequency = null,
    string? PushFrequency = null,
    TimeOnly? QuietHoursStart = null,
    TimeOnly? QuietHoursEnd = null,
    string? TimeZone = null,
    Dictionary<string, NotificationCategorySettingsDto>? CategorySettings = null
);

/// <summary>
///     Request model for bulk notification operations
/// </summary>
/// <param name="NotificationIds">IDs of notifications to operate on</param>
/// <param name="Operation">Operation to perform</param>
/// <param name="FilterCriteria">Filter criteria for selecting notifications</param>
public sealed record BulkNotificationRequest(List<Guid>? NotificationIds = null, string? Operation = null, NotificationFilterCriteria? FilterCriteria = null);

/// <summary>
///     Filter criteria for notifications
/// </summary>
/// <param name="Categories">Filter by categories</param>
/// <param name="Priorities">Filter by priorities</param>
/// <param name="Types">Filter by types</param>
/// <param name="IsRead">Filter by read status</param>
/// <param name="IsArchived">Filter by archived status</param>
/// <param name="DateFrom">Filter by creation date from</param>
/// <param name="DateTo">Filter by creation date to</param>
public record NotificationFilterCriteria(
    List<string>? Categories = null,
    List<string>? Priorities = null,
    List<string>? Types = null,
    bool? IsRead = null,
    bool? IsArchived = null,
    DateTimeOffset? DateFrom = null,
    DateTimeOffset? DateTo = null
);
