using System;

namespace GameGuild.Modules.Notifications;

/// <summary>
/// Notification entity representing a user notification.
/// </summary>
public sealed class Notification {
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid UserId { get; init; }
    public required Guid TenantId { get; init; }
    public required NotificationType Type { get; init; }
    public required NotificationPriority Priority { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
    public NotificationChannel Channel { get; init; }
    public string? ActionUrl { get; init; }
    public string? ImageUrl { get; init; }
    public string? Data { get; init; } // JSON metadata
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public bool IsDelivered { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; init; }
    public string? TemplateId { get; init; }
}

/// <summary>
/// Notification types.
/// </summary>
public enum NotificationType {
    Info = 0,
    Success = 1,
    Warning = 2,
    Error = 3,
    System = 4,
    Announcement = 5,
    Alert = 6
}

/// <summary>
/// Notification priority levels.
/// </summary>
public enum NotificationPriority {
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Notification status.
/// </summary>
public enum NotificationStatus {
    Unread = 0,
    Read = 1,
    Archived = 2
}

/// <summary>
/// Notification delivery channels.
/// </summary>
[Flags]
public enum NotificationChannel {
    None = 0,
    InApp = 1,
    Email = 2,
    Push = 4,
    Sms = 8,
    All = InApp | Email | Push | Sms
}
