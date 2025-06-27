using GameGuild.Modules.Comment.Models;


namespace GameGuild.Common.Entities;

/// <summary>
/// Represents a comment on a commentable entity.
/// </summary>
public class Comment : ResourceBase {
  private string _content = string.Empty;

  private ICollection<CommentPermission> _permissions = new List<CommentPermission>();

  public string Content {
    get => _content;
    set => _content = value;
  }

  /// <summary>
  /// Navigation to comment permissions
  /// </summary>
  public virtual ICollection<CommentPermission> Permissions {
    get => _permissions;
    set => _permissions = value;
  }
}
