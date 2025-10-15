using GameGuild.Common;


namespace GameGuild.Modules.Posts.Models;

/// <summary>
/// Represents a tag that can be applied to posts
/// </summary>
[Table("post_tags")]
[Index(nameof(Name))]
[Index(nameof(Category))]
[Index(nameof(UsageCount))]
public class PostTag : Entity {
  /// <summary>
  /// Tag name (unique)
  /// </summary>
  [Required]
  [MaxLength(50)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// Tag display name (can have spaces, special chars)
  /// </summary>
  [MaxLength(100)]
  public string DisplayName { get; set; } = string.Empty;

  /// <summary>
  /// Tag description
  /// </summary>
  [MaxLength(500)]
  public string? Description { get; set; }

  /// <summary>
  /// Tag category (e.g., "technology", "genre", "platform")
  /// </summary>
  [MaxLength(50)]
  public string Category { get; set; } = "general";

  /// <summary>
  /// Hex color for tag display
  /// </summary>
  [MaxLength(7)]
  public string? Color { get; set; }

  /// <summary>
  /// Number of posts using this tag
  /// </summary>
  public int UsageCount { get; set; } = 0;

  /// <summary>
  /// Whether tag is featured/promoted
  /// </summary>
  public bool IsFeatured { get; set; } = false;

  /// <summary>
  /// Posts that have this tag
  /// </summary>
  public virtual ICollection<PostTagAssignment> Posts { get; set; } = new List<PostTagAssignment>();
}

/// <summary>
/// Many-to-many relationship between Posts and Tags
/// </summary>
[Table("post_tag_assignments")]
[Index(nameof(PostId))]
[Index(nameof(TagId))]
public class PostTagAssignment : Entity {
  /// <summary>
  /// The post
  /// </summary>
  public Guid PostId { get; set; }

  public virtual Post Post { get; set; } = null!;

  /// <summary>
  /// The tag
  /// </summary>
  public Guid TagId { get; set; }

  public virtual PostTag Tag { get; set; } = null!;

  /// <summary>
  /// Order/priority of this tag for the post
  /// </summary>
  public int Order { get; set; } = 0;
}
