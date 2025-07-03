using GameGuild.Modules.Resources.Models;


namespace GameGuild.Modules.Comments.Models;

/// <summary>
/// Represents a comment on a commentable entity.
/// </summary>
public class Comment : ResourceBase {
  public string Content { get; set; } = string.Empty;

  /// <summary>
  /// Navigation to comment permissions
  /// </summary>
  public virtual ICollection<CommentPermission> Permissions { get; set; } = new List<CommentPermission>();
}
