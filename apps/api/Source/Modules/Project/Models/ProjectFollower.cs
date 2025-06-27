using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Project.Models;

/// <summary>
/// Represents a user following a project
/// </summary>
[Table("ProjectFollowers")]
[Index(nameof(ProjectId), nameof(UserId), IsUnique = true, Name = "IX_ProjectFollowers_Project_User")]
[Index(nameof(UserId), Name = "IX_ProjectFollowers_User")]
[Index(nameof(FollowedAt), Name = "IX_ProjectFollowers_Date")]
public class ProjectFollower : ResourceBase {
  private Guid _projectId;

  private Project _project = null!;

  private Guid _userId;

  private User.Models.User _user = null!;

  private DateTime _followedAt = DateTime.UtcNow;

  private string? _notificationSettings;

  private bool _emailNotifications = true;

  private bool _pushNotifications = true;

  /// <summary>
  /// Project being followed
  /// </summary>
  public Guid ProjectId {
    get => _projectId;
    set => _projectId = value;
  }

  /// <summary>
  /// Navigation property to project
  /// </summary>
  public virtual Project Project {
    get => _project;
    set => _project = value;
  }

  /// <summary>
  /// User following the project
  /// </summary>
  public Guid UserId {
    get => _userId;
    set => _userId = value;
  }

  /// <summary>
  /// Navigation property to user
  /// </summary>
  public virtual User.Models.User User {
    get => _user;
    set => _user = value;
  }

  /// <summary>
  /// Date when the user started following
  /// </summary>
  public DateTime FollowedAt {
    get => _followedAt;
    set => _followedAt = value;
  }

  /// <summary>
  /// Notification preferences for this follow
  /// </summary>
  [MaxLength(1000)]
  public string? NotificationSettings {
    get => _notificationSettings;
    set => _notificationSettings = value;
  }

  /// <summary>
  /// Whether the follower wants email notifications
  /// </summary>
  public bool EmailNotifications {
    get => _emailNotifications;
    set => _emailNotifications = value;
  }

  /// <summary>
  /// Whether the follower wants push notifications
  /// </summary>
  public bool PushNotifications {
    get => _pushNotifications;
    set => _pushNotifications = value;
  }
}
