using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Core.Entities;

namespace GameGuild.Modules.Notifications.Models;

/// <summary>
/// User preferences for notifications
/// </summary>
[Table("NotificationPreferences")]
public class NotificationPreferences : EntityBase
{
    /// <summary>
    /// User these preferences belong to
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Enable email notifications
    /// </summary>
    public bool EmailNotifications { get; set; } = true;

    /// <summary>
    /// Enable push notifications
    /// </summary>
    public bool PushNotifications { get; set; } = true;

    /// <summary>
    /// Enable in-app notifications
    /// </summary>
    public bool InAppNotifications { get; set; } = true;

    /// <summary>
    /// Enable sound for notifications
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Enable comment notifications
    /// </summary>
    public bool CommentNotifications { get; set; } = true;

    /// <summary>
    /// Enable follow notifications
    /// </summary>
    public bool FollowNotifications { get; set; } = true;

    /// <summary>
    /// Enable invite notifications
    /// </summary>
    public bool InviteNotifications { get; set; } = true;

    /// <summary>
    /// Enable reminder notifications
    /// </summary>
    public bool ReminderNotifications { get; set; } = true;

    /// <summary>
    /// Enable task notifications
    /// </summary>
    public bool TaskNotifications { get; set; } = true;

    /// <summary>
    /// Enable mention notifications
    /// </summary>
    public bool MentionNotifications { get; set; } = true;

    /// <summary>
    /// Enable system notifications
    /// </summary>
    public bool SystemNotifications { get; set; } = true;

    /// <summary>
    /// Enable course notifications
    /// </summary>
    public bool CourseNotifications { get; set; } = true;

    /// <summary>
    /// Enable achievement notifications
    /// </summary>
    public bool AchievementNotifications { get; set; } = true;

    /// <summary>
    /// Enable social notifications
    /// </summary>
    public bool SocialNotifications { get; set; } = true;

    /// <summary>
    /// Enable promotion notifications
    /// </summary>
    public bool PromotionNotifications { get; set; } = true;

    /// <summary>
    /// Check if a notification type is enabled
    /// </summary>
    public bool IsTypeEnabled(NotificationType type)
    {
        return type switch
        {
            NotificationType.Comment => CommentNotifications,
            NotificationType.Follow => FollowNotifications,
            NotificationType.Invite => InviteNotifications,
            NotificationType.Reminder => ReminderNotifications,
            NotificationType.Task => TaskNotifications,
            NotificationType.Mention => MentionNotifications,
            NotificationType.System => SystemNotifications,
            NotificationType.Course => CourseNotifications,
            NotificationType.Achievement => AchievementNotifications,
            NotificationType.Social => SocialNotifications,
            NotificationType.Promotion => PromotionNotifications,
            _ => true
        };
    }
}
