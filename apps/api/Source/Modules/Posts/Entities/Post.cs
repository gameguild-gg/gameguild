using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Posts.Models;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Represents a social media post within the community that follows the Content architecture
/// </summary>
[Table("posts")]
[Index(nameof(PostType))]
[Index(nameof(IsSystemGenerated))]
[Index(nameof(IsPinned))]
[Index(nameof(AuthorId))]
public class Post : Content {
  /// <summary>
  /// Type of post (e.g., "user_signup", "achievement", "general", "announcement", etc.)
  /// </summary>
  [MaxLength(50)]
  public string PostType { get; set; } = "general";

  /// <summary>
  /// ID of the user who authored/created the post
  /// </summary>
  public Guid? AuthorId { get; set; }

  /// <summary>
  /// Navigation property to the author
  /// </summary>
  public virtual User? Author { get; set; }

  /// <summary>
  /// Whether this post is system-generated (welcome posts, achievements) or user-created
  /// </summary>
  public bool IsSystemGenerated { get; set; } = false;

  /// <summary>
  /// Whether the post is pinned (appears at top of feeds)
  /// </summary>
  public bool IsPinned { get; set; } = false;

  /// <summary>
  /// Number of likes/reactions on this post
  /// </summary>
  public int LikesCount { get; set; } = 0;

  /// <summary>
  /// Number of comments on this post
  /// </summary>
  public int CommentsCount { get; set; } = 0;

  /// <summary>
  /// Number of shares/reposts
  /// </summary>
  public int SharesCount { get; set; } = 0;

  /// <summary>
  /// Rich content stored as JSON (images, links, embeds, etc.)
  /// </summary>
  [Column(TypeName = "jsonb")]
  public string? RichContent { get; set; }

  /// <summary>
  /// References to other content/resources this post relates to
  /// </summary>
  public virtual ICollection<PostContentReference> ContentReferences { get; set; } = new List<PostContentReference>();

  /// <summary>
  /// Comments on this post
  /// </summary>
  public virtual ICollection<PostComment> Comments { get; set; } = new List<PostComment>();

  /// <summary>
  /// Likes/reactions on this post
  /// </summary>
  public virtual ICollection<PostLike> Likes { get; set; } = new List<PostLike>();

  /// <summary>
  /// Tags associated with this post
  /// </summary>
  public virtual ICollection<PostTagAssignment> Tags { get; set; } = new List<PostTagAssignment>();

  /// <summary>
  /// Users following this post for notifications
  /// </summary>
  public virtual ICollection<PostFollower> Followers { get; set; } = new List<PostFollower>();

  /// <summary>
  /// Views/visits to this post
  /// </summary>
  public virtual ICollection<PostView> Views { get; set; } = new List<PostView>();

  /// <summary>
  /// Statistics for this post
  /// </summary>
  public virtual PostStatistics? Statistics { get; set; }

  /// <summary>
  /// Slug for URL-friendly identification (inherited from Content base class)
  /// Auto-generated from title if not provided
  /// </summary>
  public static string GenerateSlug(string title) {
    if (string.IsNullOrWhiteSpace(title)) return Guid.NewGuid().ToString("N")[..8];

    return title.ToLowerInvariant()
                .Replace(" ", "-")
                .Replace("_", "-")
                .Where(c => char.IsLetterOrDigit(c) || c == '-')
                .Aggregate("", (current, c) => current + c)
                .Trim('-');
  }
}
