using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Represents a reference from a post to other content/resources
/// </summary>
[Table("post_content_references")]
[Index(nameof(PostId))]
[Index(nameof(ReferencedResourceId))]
public class PostContentReference : Entity {
  /// <summary>
  /// The post that contains this reference
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The resource being referenced
  /// </summary>
  public Guid ReferencedResourceId { get; set; }

  public virtual Resource ReferencedResource { get; set; } = null!;

  /// <summary>
  /// Type of reference (mention, attachment, related_content, etc.)
  /// </summary>
  [MaxLength(50)]
  public string ReferenceType { get; set; } = "mention";

  /// <summary>
  /// Optional context about this reference
  /// </summary>
  [MaxLength(500)]
  public string? Context { get; set; }
}

/// <summary>
/// Represents a comment on a post
/// </summary>
[Table("post_comments")]
[Index(nameof(PostId))]
[Index(nameof(AuthorId))]
[Index(nameof(ParentCommentId))]
public class PostComment : Entity {
  /// <summary>
  /// The post this comment belongs to
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The user who wrote the comment
  /// </summary>
  public Guid AuthorId { get; set; }

  public virtual User Author { get; set; } = null!;

  /// <summary>
  /// The comment content
  /// </summary>
  [Required]
  [MaxLength(1000)]
  public string Content { get; set; } = string.Empty;

  /// <summary>
  /// Parent comment for threaded replies (null for top-level comments)
  /// </summary>
  public Guid? ParentCommentId { get; set; }

  public virtual PostComment? ParentComment { get; set; }

  /// <summary>
  /// Child replies to this comment
  /// </summary>
  public virtual ICollection<PostComment> Replies { get; set; } = new List<PostComment>();

  /// <summary>
  /// Number of likes on this comment
  /// </summary>
  public int LikesCount { get; set; } = 0;
}

/// <summary>
/// Represents a like/reaction on a post
/// </summary>
[Table("post_likes")]
[Index(nameof(PostId))]
[Index(nameof(UserId))]
[Index(nameof(PostId), nameof(UserId), IsUnique = true)] // Prevent duplicate likes
public class PostLike : Entity {
  /// <summary>
  /// The post being liked
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The user who liked the post
  /// </summary>
  public Guid UserId { get; set; }

  public virtual User User { get; set; } = null!;

  /// <summary>
  /// Type of reaction (like, love, laugh, etc.)
  /// </summary>
  [MaxLength(20)]
  public string ReactionType { get; set; } = "like";
}
