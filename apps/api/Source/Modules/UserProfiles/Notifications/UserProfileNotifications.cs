using MediatR;

namespace GameGuild.Modules.UserProfiles.Notifications;

/// <summary>
/// Notification sent when a user profile is created
/// </summary>
public class UserProfileCreatedNotification : INotification
{
    public Guid UserProfileId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Notification sent when a user profile is updated
/// </summary>
public class UserProfileUpdatedNotification : INotification
{
    public Guid UserProfileId { get; set; }
    public Guid UserId { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Changes { get; set; } = new();
}

/// <summary>
/// Notification sent when a user profile is deleted
/// </summary>
public class UserProfileDeletedNotification : INotification
{
    public Guid UserProfileId { get; set; }
    public Guid UserId { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    public bool SoftDelete { get; set; } = true;
}

/// <summary>
/// Notification sent when a user profile is restored
/// </summary>
public class UserProfileRestoredNotification : INotification
{
    public Guid UserProfileId { get; set; }
    public Guid UserId { get; set; }
    public DateTime RestoredAt { get; set; } = DateTime.UtcNow;
}
