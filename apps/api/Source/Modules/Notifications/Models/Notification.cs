using System.Text.Json;


namespace GameGuild.Modules.Notifications.Models;

/// <summary>
/// Represents a notification sent to a user
/// </summary>
[Table("Notifications")]
public class Notification : EntityBase
{
    /// <summary>
    /// User who receives this notification
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// User who triggered this notification (optional)
    /// </summary>
    public Guid? FromUserId { get; set; }

    /// <summary>
    /// Tenant context for the notification
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// Type of notification
    /// </summary>
    [Required]
    public NotificationType Type { get; set; }

    /// <summary>
    /// Priority level of the notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Medium;

    /// <summary>
    /// Status of the notification
    /// </summary>
    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

    /// <summary>
    /// Title of the notification
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Main message content
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Whether the notification is starred/bookmarked
    /// </summary>
    public bool IsStarred { get; set; }

    /// <summary>
    /// URL to navigate to when notification is clicked
    /// </summary>
    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Text for the action button
    /// </summary>
    [MaxLength(50)]
    public string? ActionText { get; set; }

    /// <summary>
    /// When the notification was read
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// When the notification was archived
    /// </summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>
    /// Metadata as JSON for extensibility
    /// </summary>
    public string? MetadataJson { get; set; }

    /// <summary>
    /// Get metadata as dictionary
    /// </summary>
    public Dictionary<string, object>? GetMetadata()
    {
        if (string.IsNullOrEmpty(MetadataJson)) return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(MetadataJson);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Set metadata from dictionary
    /// </summary>
    public void SetMetadata(Dictionary<string, object>? metadata)
    {
        if (metadata == null)
        {
            MetadataJson = null;
            return;
        }

        try
        {
            MetadataJson = JsonSerializer.Serialize(metadata);
        }
        catch
        {
            MetadataJson = null;
        }
    }

    /// <summary>
    /// Mark notification as read
    /// </summary>
    public void MarkAsRead()
    {
        if (Status != NotificationStatus.Read)
        {
            Status = NotificationStatus.Read;
            ReadAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Mark notification as archived
    /// </summary>
    public void MarkAsArchived()
    {
        if (Status != NotificationStatus.Archived)
        {
            Status = NotificationStatus.Archived;
            ArchivedAt = DateTime.UtcNow;
            ReadAt ??= DateTime.UtcNow; // Also mark as read
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Toggle starred status
    /// </summary>
    public void ToggleStar()
    {
        IsStarred = !IsStarred;
        UpdatedAt = DateTime.UtcNow;
    }
}
