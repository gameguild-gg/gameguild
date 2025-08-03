using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Projects;

/// <summary>
/// Represents a user following a project
/// </summary>
[Table("ProjectFollowers")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectFollowers_Project_User")]
[Index(nameof(UserId), Name = "IX_ProjectFollowers_User")]
[Index(nameof(FollowedAt), Name = "IX_ProjectFollowers_Date")]
public class ProjectFollower : Resource {
  /// <summary>
  /// Project being followed
  /// </summary>
  public Guid ProjectId { get; set; }

  /// <summary>
  /// Navigation property to project
  /// </summary>
  public virtual Project Project { get; set; } = null!;

  /// <summary>
  /// User following the project
  /// </summary>
  public Guid UserId { get; set; }

  /// <summary>
  /// Navigation property to user
  /// </summary>
  public virtual User? User { get; set; }

  /// <summary>
  /// Date when the user started following
  /// </summary>
  public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// Notification preferences for this follow
  /// </summary>
  [MaxLength(1000)]
  public string? NotificationSettings { get; set; }

  /// <summary>
  /// Whether the follower wants email notifications
  /// </summary>
  public bool EmailNotifications { get; set; } = true;

  /// <summary>
  /// Whether the follower wants push notifications
  /// </summary>
  public bool PushNotifications { get; set; } = true;
}
