using GameGuild.Modules.Resources;


namespace GameGuild.Modules.Comments;

/// <summary>
/// Represents a comment on a commentable entity.
/// </summary>
public class Comment : Resource {
  public string Content { get; set; } = string.Empty;

  /// <summary>
  /// Navigation to comment permissions
  /// </summary>
  public virtual ICollection<CommentPermission> Permissions { get; set; } = new List<CommentPermission>();
}
