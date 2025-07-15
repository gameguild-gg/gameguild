using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Posts;

/// <summary>
/// DTO for creating a new post
/// </summary>
public class CreatePostDto {
  public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  public string PostType { get; set; } = "general";

  public AccessLevel Visibility { get; set; } = AccessLevel.Public;

  public string? RichContent { get; set; }

  public List<Guid>? ContentReferences { get; set; }
}

/// <summary>
/// DTO for updating an existing post
/// </summary>
public class UpdatePostDto {
  public string? Title { get; set; }

  public string? Description { get; set; }

  public AccessLevel? Visibility { get; set; }

  public bool? IsPinned { get; set; }

  public string? RichContent { get; set; }

  public ContentStatus? Status { get; set; }
}

/// <summary>
/// DTO for post responses
/// </summary>
public class PostDto {
  public Guid Id { get; set; }

  public string Title { get; set; } = string.Empty;

  public string? Description { get; set; }

  public string Slug { get; set; } = string.Empty;

  public string PostType { get; set; } = string.Empty;

  public Guid AuthorId { get; set; }

  public string AuthorName { get; set; } = string.Empty;

  public bool IsSystemGenerated { get; set; }

  public AccessLevel Visibility { get; set; }

  public ContentStatus Status { get; set; }

  public bool IsPinned { get; set; }

  public int LikesCount { get; set; }

  public int CommentsCount { get; set; }

  public int SharesCount { get; set; }

  public string? RichContent { get; set; }

  public List<ContentReferenceDto> ContentReferences { get; set; } = new();

  public DateTime CreatedAt { get; set; }

  public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for content references within posts
/// </summary>
public class ContentReferenceDto {
  public Guid ResourceId { get; set; }

  public string ResourceTitle { get; set; } = string.Empty;

  public string ResourceType { get; set; } = string.Empty;

  public string ReferenceType { get; set; } = string.Empty;

  public string? Context { get; set; }
}

/// <summary>
/// DTO for post comments
/// </summary>
public class PostCommentDto {
  public Guid Id { get; set; }

  public string Content { get; set; } = string.Empty;

  public Guid AuthorId { get; set; }

  public string AuthorName { get; set; } = string.Empty;

  public Guid? ParentCommentId { get; set; }

  public int LikesCount { get; set; }

  public DateTime CreatedAt { get; set; }

  public List<PostCommentDto> Replies { get; set; } = new();
}

/// <summary>
/// DTO for creating comments
/// </summary>
public class CreateCommentDto {
  public string Content { get; set; } = string.Empty;

  public Guid? ParentCommentId { get; set; }
}

/// <summary>
/// DTO for paginated post results
/// </summary>
public class PostsPageDto {
  public List<PostDto> Posts { get; set; } = new();

  public int TotalCount { get; set; }

  public int PageNumber { get; set; }

  public int PageSize { get; set; }

  public bool HasNextPage { get; set; }

  public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO for creating system announcements
/// </summary>
public class CreateAnnouncementDto {
  public string Title { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;
}

/// <summary>
/// DTO for creating milestone celebration posts
/// </summary>
public class CreateMilestoneDto {
  public string Milestone { get; set; } = string.Empty;

  public string Description { get; set; } = string.Empty;

  public Guid? UserId { get; set; } // If null, uses current user
}
