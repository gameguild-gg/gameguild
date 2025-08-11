using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts.Models;

/// <summary>
/// Represents a view/visit to a post for analytics
/// </summary>
[Table("post_views")]
[Index(nameof(PostId))]
[Index(nameof(UserId))]
[Index(nameof(ViewedAt))]
[Index(nameof(IpAddress))]
public class PostView : Entity {
  /// <summary>
  /// The post that was viewed
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The user who viewed (nullable for anonymous views)
  /// </summary>
  public Guid? UserId { get; set; }

  public virtual User? User { get; set; }

  /// <summary>
  /// When the view occurred
  /// </summary>
  public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// IP address of viewer (for unique view tracking)
  /// </summary>
  [MaxLength(45)] // IPv6 max length
  public string? IpAddress { get; set; }

  /// <summary>
  /// User agent string
  /// </summary>
  [MaxLength(500)]
  public string? UserAgent { get; set; }

  /// <summary>
  /// Referrer URL
  /// </summary>
  [MaxLength(500)]
  public string? Referrer { get; set; }

  /// <summary>
  /// Duration of view in seconds
  /// </summary>
  public int DurationSeconds { get; set; } = 0;

  /// <summary>
  /// Whether this is considered an engaged view (long duration, interactions)
  /// </summary>
  public bool IsEngaged { get; set; } = false;
}
