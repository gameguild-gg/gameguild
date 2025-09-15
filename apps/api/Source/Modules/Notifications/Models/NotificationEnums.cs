namespace GameGuild.Modules.Notifications.Models;

/// <summary>
/// Types of notifications supported by the system
/// </summary>
public enum NotificationType
{
    Comment = 0,
    Follow = 1,
    Invite = 2,
    Reminder = 3,
    Task = 4,
    Mention = 5,
    System = 6,
    Course = 7,
    Achievement = 8,
    Social = 9,
    Promotion = 10
}

/// <summary>
/// Priority levels for notifications
/// </summary>
public enum NotificationPriority
{
    Low = 0,
    Medium = 1,
    High = 2
}

/// <summary>
/// Status of notifications
/// </summary>
public enum NotificationStatus
{
    Unread = 0,
    Read = 1,
    Archived = 2
}
