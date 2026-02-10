using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Identity.Users;

/// <summary>
///     User notifications entity for storing notification history and settings
/// </summary>
[Table("UserNotifications")]
[Index(nameof(UserId))]
[Index(nameof(Type))]
[Index(nameof(IsRead))]
[Index(nameof(Priority))]
[Index(nameof(CreatedAt))]
public class UserNotification : EntityBase
{
    /// <summary>
    ///     Default constructor
    /// </summary>
    public UserNotification() { }

    /// <summary>
    ///     Constructor for partial initialization
    /// </summary>
    /// <param name="partial">Partial notification data</param>
    public UserNotification(object partial) : base(partial) { }

    /// <summary>
    ///     ID of the user this notification belongs to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    ///     Navigation property to the user
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    ///     Notification type/category
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    ///     Notification title
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Notification content/message
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    ///     Priority level
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    ///     Whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    ///     Whether the notification is archived
    /// </summary>
    public bool IsArchived { get; set; }

    /// <summary>
    ///     When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    ///     When the notification was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    ///     Sender ID (if applicable)
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    ///     Source system or module that generated the notification
    /// </summary>
    [MaxLength(100)]
    public string? Source { get; set; }

    /// <summary>
    ///     Related entity ID (if applicable)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    ///     Related entity type (if applicable)
    /// </summary>
    [MaxLength(100)]
    public string? RelatedEntityType { get; set; }

    /// <summary>
    ///     Action URL or deep link
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    ///     Additional metadata as JSON
    /// </summary>
    [Column(TypeName = "jsonb")]
    [MaxLength(10000)]
    public string? Metadata { get; set; }

    /// <summary>
    ///     Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = SystemClock.UtcNow;
            Touch();
        }
    }

    /// <summary>
    ///     Mark notification as unread
    /// </summary>
    public void MarkAsUnread()
    {
        if (IsRead)
        {
            IsRead = false;
            ReadAt = null;
            Touch();
        }
    }

    /// <summary>
    ///     Archive the notification
    /// </summary>
    public void Archive()
    {
        if (!IsArchived)
        {
            IsArchived = true;
            ArchivedAt = SystemClock.UtcNow;
            Touch();
        }
    }

    /// <summary>
    ///     Unarchive the notification
    /// </summary>
    public void Unarchive()
    {
        if (IsArchived)
        {
            IsArchived = false;
            ArchivedAt = null;
            Touch();
        }
    }

    /// <summary>
    ///     Factory method to create notification
    /// </summary>
    public static UserNotification Create(Guid userId, string type, string title, string content, NotificationPriority priority = NotificationPriority.Normal, Guid? senderId = null, string? source = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new UserNotification { UserId = userId, Type = type, Title = title, Content = content, Priority = priority, SenderId = senderId, Source = source };
    }
}

/// <summary>
///     Notification priority levels
/// </summary>
public enum NotificationPriority { Low = 0, Normal = 1, High = 2, Urgent = 3, Critical = 4 }
