using GameGuild.Common;
using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Posts;

/// <summary>
/// Command to create a new post
/// </summary>
public class CreatePostCommand : ICommand<Common.Result<Post>> {
  public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  public string PostType { get; set; } = "general";

  public Guid AuthorId { get; set; }

  public bool IsSystemGenerated { get; set; } = false;

  public AccessLevel Visibility { get; set; } = AccessLevel.Public;

  public string? RichContent { get; set; }

  public List<Guid>? ContentReferences { get; set; }

  public Guid? TenantId { get; set; }
}

/// <summary>
/// Command to update an existing post
/// </summary>
public class UpdatePostCommand : ICommand<Common.Result<Post>> {
  public Guid PostId { get; set; }

  public string? Title { get; set; }

  public string? Description { get; set; }

  public AccessLevel? Visibility { get; set; }

  public bool? IsPinned { get; set; }

  public string? RichContent { get; set; }

  public ContentStatus? Status { get; set; }

  public Guid UserId { get; set; } // For authorization
}

/// <summary>
/// Command to delete a post
/// </summary>
public class DeletePostCommand : ICommand<Common.Result<bool>> {
  public Guid PostId { get; set; }

  public Guid UserId { get; set; } // For authorization
}

/// <summary>
/// Command to like/unlike a post
/// </summary>
public class TogglePostLikeCommand : ICommand<Common.Result<bool>> {
  public Guid PostId { get; set; }

  public Guid UserId { get; set; }

  public string ReactionType { get; set; } = "like";
}

/// <summary>
/// Command to add a comment to a post
/// </summary>
public class AddCommentCommand : ICommand<Common.Result<PostComment>> {
  public Guid PostId { get; set; }

  public Guid AuthorId { get; set; }

  public string Content { get; set; } = string.Empty;

  public Guid? ParentCommentId { get; set; }
}

/// <summary>
/// Command to pin/unpin a post (admin only)
/// </summary>
public class TogglePostPinCommand : ICommand<Common.Result<Post>> {
  public Guid PostId { get; set; }

  public Guid UserId { get; set; } // For authorization
}
