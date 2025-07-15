using GameGuild.Modules.Contents;


namespace GameGuild.Modules.Posts.GraphQL;

/// <summary>
/// Input for adding comments to posts
/// </summary>
public class AddCommentInput
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid? ParentCommentId { get; set; }
}

/// <summary>
/// Input for creating post tags
/// </summary>
public class CreateTagInput
{
    public string Name { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Color { get; set; }
    public bool? IsFeatured { get; set; }
}

/// <summary>
/// Enhanced create post input with additional fields
/// </summary>
public class CreatePostInputEnhanced
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PostType { get; set; }
    public Guid? UserId { get; set; }
    public AccessLevel? Visibility { get; set; }
    public ContentStatus? Status { get; set; }
    public bool? IsPinned { get; set; }
    public string? RichContent { get; set; }
    public string[]? Tags { get; set; }
    public Guid? TenantId { get; set; }
    public List<Guid>? ContentReferences { get; set; }
}

/// <summary>
/// Enhanced update post input with additional fields
/// </summary>
public class UpdatePostInputEnhanced
{
    public Guid PostId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public AccessLevel? Visibility { get; set; }
    public ContentStatus? Status { get; set; }
    public bool? IsPinned { get; set; }
    public string? RichContent { get; set; }
    public string[]? Tags { get; set; }
}

/// <summary>
/// Input for following/unfollowing posts
/// </summary>
public class PostFollowInput
{
    public Guid PostId { get; set; }
    public Guid UserId { get; set; }
    public bool NotifyOnComments { get; set; } = true;
    public bool NotifyOnLikes { get; set; } = false;
    public bool NotifyOnShares { get; set; } = false;
    public bool NotifyOnUpdates { get; set; } = true;
}

/// <summary>
/// Input for recording post views
/// </summary>
public class RecordViewInput
{
    public Guid PostId { get; set; }
    public Guid? UserId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Referrer { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public bool IsEngaged { get; set; } = false;
}

/// <summary>
/// Input for bulk operations on posts
/// </summary>
public class BulkPostOperationInput
{
    public Guid[] PostIds { get; set; } = Array.Empty<Guid>();
    public string Operation { get; set; } = string.Empty; // "delete", "restore", "pin", "unpin"
    public object? Parameters { get; set; }
}

/// <summary>
/// Input for post search with advanced filters
/// </summary>
public class PostSearchInput
{
    public string? SearchTerm { get; set; }
    public string[]? Tags { get; set; }
    public string[]? PostTypes { get; set; }
    public Guid[]? AuthorIds { get; set; }
    public AccessLevel[]? VisibilityLevels { get; set; }
    public ContentStatus[]? Statuses { get; set; }
    public bool? IsSystemGenerated { get; set; }
    public bool? IsPinned { get; set; }
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public int MinLikes { get; set; } = 0;
    public int MinComments { get; set; } = 0;
    public int MinShares { get; set; } = 0;
    public string SortBy { get; set; } = "createdAt"; // "createdAt", "likes", "comments", "trending"
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// Output for post operations with metadata
/// </summary>
public class PostOperationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Post? Post { get; set; }
    public int AffectedCount { get; set; } = 0;
    public Dictionary<string, object>? Metadata { get; set; }
}
