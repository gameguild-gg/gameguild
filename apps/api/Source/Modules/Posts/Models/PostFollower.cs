using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts.Models;

/// <summary>
/// Represents a user following a post for notifications
/// </summary>
[Table("post_followers")]
[Index(nameof(PostId))]
[Index(nameof(UserId))]
[Index(nameof(CreatedAt))]
public class PostFollower : Entity {
  /// <summary>
  /// The post being followed
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The user following the post
  /// </summary>
  public Guid UserId { get; set; }

  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Whether to receive notifications for comments
  /// </summary>
  public bool NotifyOnComments { get; set; } = true;

  /// <summary>
  /// Whether to receive notifications for likes
  /// </summary>
  public bool NotifyOnLikes { get; set; } = false;

  /// <summary>
  /// Whether to receive notifications for shares
  /// </summary>
  public bool NotifyOnShares { get; set; } = false;

  /// <summary>
  /// Whether to receive notifications for updates
  /// </summary>
  public bool NotifyOnUpdates { get; set; } = true;
}
