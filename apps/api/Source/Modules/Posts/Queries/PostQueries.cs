using GameGuild.Common;

namespace GameGuild.Modules.Posts;

/// <summary>
/// Query to get posts with pagination and filtering
/// </summary>
public class GetPostsQuery : IQuery<Common.Result<PostsPageDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? PostType { get; set; }
    public Guid? UserId { get; set; }
    public bool? IsPublic { get; set; } = true;
    public bool? IsPinned { get; set; }
    public List<string>? Tags { get; set; }
    public Guid? TenantId { get; set; }
    public string? SearchTerm { get; set; }
    public string OrderBy { get; set; } = "CreatedAt"; // CreatedAt, LikesCount, CommentsCount
    public bool Descending { get; set; } = true;
}

/// <summary>
/// Query to get a single post by ID
/// </summary>
public class GetPostByIdQuery : IQuery<Common.Result<Post>>
{
    public Guid PostId { get; set; }
    public Guid? TenantId { get; set; }
}

/// <summary>
/// Query to get posts by a specific user
/// </summary>
public class GetUserPostsQuery : IQuery<Common.Result<PostsPageDto>>
{
    public Guid UserId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsPublic { get; set; } = true;
    public Guid TenantId { get; set; }
}

/// <summary>
/// Query to get trending posts (most liked/commented recently)
/// </summary>
public class GetTrendingPostsQuery : IQuery<Common.Result<PostsPageDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid TenantId { get; set; }
    public int HoursBack { get; set; } = 24; // Look at posts from last 24 hours
}
