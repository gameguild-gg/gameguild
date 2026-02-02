using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GameGuild.Identity.Context.Actors;
using GameGuild.Social.Posts.Services;

namespace GameGuild.Social.Posts.Controllers;

/// <summary>
/// REST API for post management
/// </summary>
[ApiController]
[Route("api/v1/posts")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IActorContextAccessor _actorContextAccessor;

    public PostsController(IPostService postService, IActorContextAccessor actorContextAccessor)
    {
        _postService = postService;
        _actorContextAccessor = actorContextAccessor;
    }

    private Guid GetCurrentUserId()
    {
        var actor = _actorContextAccessor.ActorContext;
        return actor?.SubjectIdAsGuid ?? Guid.Empty;
    }

    #region Posts CRUD

    /// <summary>Get paginated list of public posts</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPublicPostsAsync(skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Get a single post by ID</summary>
    [HttpGet("{postId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(Guid postId, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostByIdAsync(postId, cancellationToken);
        return result.IsSuccess
            ? Ok(MapToDto(result.Value!))
            : NotFound(result.Error);
    }

    /// <summary>Get posts for the current user's feed</summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.GetFeedPostsAsync(userId, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Get trending posts</summary>
    [HttpGet("trending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetTrending([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetTrendingPostsAsync(skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Get posts by a specific author</summary>
    [HttpGet("author/{authorId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByAuthor(Guid authorId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostsByAuthorAsync(authorId, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Get current user's posts</summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyPosts([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.GetPostsByAuthorAsync(userId, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Create a new post</summary>
    [HttpPost]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.CreatePostAsync(
            userId,
            request.Content,
            request.Visibility,
            request.MediaUrl,
            request.MediaType,
            request.TenantId,
            cancellationToken);

        if (result.IsSuccess && result.Value is not null && request.Tags?.Length > 0)
        {
            await _postService.AddTagsToPostAsync(result.Value.Id, request.Tags, cancellationToken);
        }

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPost), new { postId = result.Value!.Id }, MapToDto(result.Value))
            : BadRequest(result.Error);
    }

    /// <summary>Update a post</summary>
    [HttpPut("{postId:guid}")]
    public async Task<IActionResult> UpdatePost(Guid postId, [FromBody] UpdatePostRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        // Check ownership
        var canPerform = await _postService.CanUserPerformActionAsync(postId, userId, "edit", cancellationToken);
        if (!canPerform.IsSuccess || !canPerform.Value)
            return Forbid();

        var result = await _postService.UpdatePostAsync(postId, request.Content, cancellationToken);
        return result.IsSuccess
            ? Ok(MapToDto(result.Value!))
            : BadRequest(result.Error);
    }

    /// <summary>Delete a post</summary>
    [HttpDelete("{postId:guid}")]
    public async Task<IActionResult> DeletePost(Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        // Check ownership
        var canPerform = await _postService.CanUserPerformActionAsync(postId, userId, "delete", cancellationToken);
        if (!canPerform.IsSuccess || !canPerform.Value)
            return Forbid();

        var result = await _postService.DeletePostAsync(postId, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    #endregion

    #region Interactions

    /// <summary>Toggle like on a post</summary>
    [HttpPost("{postId:guid}/like")]
    public async Task<IActionResult> ToggleLike(Guid postId, [FromQuery] string reactionType = "like", CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.TogglePostLikeAsync(postId, userId, reactionType, cancellationToken);
        return result.IsSuccess
            ? Ok(new { Liked = result.Value })
            : BadRequest(result.Error);
    }

    /// <summary>Toggle pin on a post (author only)</summary>
    [HttpPost("{postId:guid}/pin")]
    public async Task<IActionResult> TogglePin(Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var canPerform = await _postService.CanUserPerformActionAsync(postId, userId, "pin", cancellationToken);
        if (!canPerform.IsSuccess || !canPerform.Value)
            return Forbid();

        var result = await _postService.TogglePostPinAsync(postId, cancellationToken);
        return result.IsSuccess
            ? Ok(new { Pinned = result.Value })
            : BadRequest(result.Error);
    }

    /// <summary>Record a share of the post</summary>
    [HttpPost("{postId:guid}/share")]
    [AllowAnonymous]
    public async Task<IActionResult> Share(Guid postId, CancellationToken cancellationToken = default)
    {
        var result = await _postService.SharePostAsync(postId, cancellationToken);
        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>Record a view of the post</summary>
    [HttpPost("{postId:guid}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordView(
        Guid postId,
        [FromHeader(Name = "X-Forwarded-For")] string? ipAddress = null,
        [FromHeader(Name = "User-Agent")] string? userAgent = null,
        [FromHeader(Name = "Referer")] string? referrer = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _postService.RecordPostViewAsync(
            postId,
            userId == Guid.Empty ? null : userId,
            ipAddress,
            userAgent,
            referrer,
            cancellationToken);

        return result.IsSuccess
            ? Ok()
            : BadRequest(result.Error);
    }

    /// <summary>Get statistics for a post</summary>
    [HttpGet("{postId:guid}/statistics")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStatistics(Guid postId, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostStatisticsAsync(postId, cancellationToken);
        return result.IsSuccess
            ? Ok(MapStatisticsToDto(result.Value!))
            : NotFound(result.Error);
    }

    #endregion

    #region Comments

    /// <summary>Get comments for a post</summary>
    [HttpGet("{postId:guid}/comments")]
    [AllowAnonymous]
    public async Task<IActionResult> GetComments(Guid postId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostCommentsAsync(postId, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapCommentToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Add a comment to a post</summary>
    [HttpPost("{postId:guid}/comments")]
    public async Task<IActionResult> AddComment(Guid postId, [FromBody] AddCommentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.AddCommentAsync(postId, userId, request.Content, request.ParentCommentId, cancellationToken);
        return result.IsSuccess
            ? Created($"/api/v1/posts/{postId}/comments/{result.Value!.Id}", MapCommentToDto(result.Value))
            : BadRequest(result.Error);
    }

    /// <summary>Update a comment</summary>
    [HttpPut("{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> UpdateComment(Guid postId, Guid commentId, [FromBody] UpdateCommentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        // TODO: Check comment ownership
        var result = await _postService.UpdateCommentAsync(commentId, request.Content, cancellationToken);
        return result.IsSuccess
            ? Ok(MapCommentToDto(result.Value!))
            : BadRequest(result.Error);
    }

    /// <summary>Delete a comment</summary>
    [HttpDelete("{postId:guid}/comments/{commentId:guid}")]
    public async Task<IActionResult> DeleteComment(Guid postId, Guid commentId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        // TODO: Check comment ownership
        var result = await _postService.DeleteCommentAsync(commentId, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    #endregion

    #region Tags

    /// <summary>Get popular tags</summary>
    [HttpGet("tags/popular")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPopularTags([FromQuery] int count = 20, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPopularTagsAsync(count, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapTagToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Get tags for a post</summary>
    [HttpGet("{postId:guid}/tags")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostTags(Guid postId, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostTagsAsync(postId, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapTagToDto))
            : BadRequest(result.Error);
    }

    /// <summary>Search posts by tags</summary>
    [HttpGet("tags/search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchByTags([FromQuery] string[] tags, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var result = await _postService.GetPostsByTagsAsync(tags, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    #endregion

    #region Post Following

    /// <summary>Follow a post for notifications</summary>
    [HttpPost("{postId:guid}/follow")]
    public async Task<IActionResult> FollowPost(Guid postId, [FromBody] FollowPostRequest? request = null, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.FollowPostAsync(
            postId,
            userId,
            request?.NotifyOnComments ?? true,
            request?.NotifyOnLikes ?? false,
            request?.NotifyOnShares ?? false,
            request?.NotifyOnUpdates ?? true,
            cancellationToken);

        return result.IsSuccess
            ? Ok(MapFollowerToDto(result.Value!))
            : BadRequest(result.Error);
    }

    /// <summary>Unfollow a post</summary>
    [HttpDelete("{postId:guid}/follow")]
    public async Task<IActionResult> UnfollowPost(Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.UnfollowPostAsync(postId, userId, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : BadRequest(result.Error);
    }

    /// <summary>Check if current user is following a post</summary>
    [HttpGet("{postId:guid}/follow")]
    public async Task<IActionResult> IsFollowing(Guid postId, CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (userId == Guid.Empty)
            return Unauthorized();

        var result = await _postService.IsFollowingPostAsync(postId, userId, cancellationToken);
        return result.IsSuccess
            ? Ok(new { IsFollowing = result.Value })
            : BadRequest(result.Error);
    }

    #endregion

    #region Search

    /// <summary>Search posts by content</summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Search query is required");

        var result = await _postService.SearchPostsAsync(q, skip, take, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value!.Select(MapToDto))
            : BadRequest(result.Error);
    }

    #endregion

    #region DTOs and Mapping

    private static PostDto MapToDto(Post post) => new()
    {
        Id = post.Id,
        AuthorId = post.AuthorId,
        TenantId = post.TenantId,
        Content = post.Content,
        MediaUrl = post.MediaUrl,
        MediaType = post.MediaType?.ToString(),
        Visibility = post.Visibility.ToString(),
        IsPinned = post.IsPinned,
        IsEdited = post.IsEdited,
        EditedAt = post.EditedAt,
        LikesCount = post.LikesCount,
        CommentsCount = post.CommentsCount,
        SharesCount = post.SharesCount,
        ViewsCount = post.ViewsCount,
        ReplyToPostId = post.ReplyToPostId,
        RepostOfPostId = post.RepostOfPostId,
        CreatedAt = post.CreatedAt,
        UpdatedAt = post.UpdatedAt
    };

    private static CommentDto MapCommentToDto(PostComment comment) => new()
    {
        Id = comment.Id,
        PostId = comment.PostId,
        AuthorId = comment.AuthorId,
        ParentCommentId = comment.ParentCommentId,
        Content = comment.Content,
        IsEdited = comment.IsEdited,
        EditedAt = comment.EditedAt,
        LikesCount = comment.LikesCount,
        CreatedAt = comment.CreatedAt
    };

    private static TagDto MapTagToDto(PostTag tag) => new()
    {
        Id = tag.Id,
        Name = tag.Name,
        DisplayName = tag.DisplayName,
        Description = tag.Description,
        Category = tag.Category,
        Color = tag.Color,
        UsageCount = tag.UsageCount,
        IsFeatured = tag.IsFeatured
    };

    private static StatisticsDto MapStatisticsToDto(PostStatistics stats) => new()
    {
        PostId = stats.PostId,
        ViewsCount = stats.ViewsCount,
        UniqueViewersCount = stats.UniqueViewersCount,
        ExternalSharesCount = stats.ExternalSharesCount,
        AverageEngagementTime = stats.AverageEngagementTime,
        EngagementScore = stats.EngagementScore,
        TrendingScore = stats.TrendingScore,
        LastCalculatedAt = stats.LastCalculatedAt
    };

    private static FollowerDto MapFollowerToDto(PostFollower follower) => new()
    {
        PostId = follower.PostId,
        UserId = follower.UserId,
        NotifyOnComments = follower.NotifyOnComments,
        NotifyOnLikes = follower.NotifyOnLikes,
        NotifyOnShares = follower.NotifyOnShares,
        NotifyOnUpdates = follower.NotifyOnUpdates,
        CreatedAt = follower.CreatedAt
    };

    #endregion
}

#region Request/Response DTOs

public record CreatePostRequest
{
    public string Content { get; init; } = string.Empty;
    public PostVisibility Visibility { get; init; } = PostVisibility.Public;
    public string? MediaUrl { get; init; }
    public MediaType? MediaType { get; init; }
    public Guid? TenantId { get; init; }
    public string[]? Tags { get; init; }
}

public record UpdatePostRequest
{
    public string Content { get; init; } = string.Empty;
}

public record AddCommentRequest
{
    public string Content { get; init; } = string.Empty;
    public Guid? ParentCommentId { get; init; }
}

public record UpdateCommentRequest
{
    public string Content { get; init; } = string.Empty;
}

public record FollowPostRequest
{
    public bool NotifyOnComments { get; init; } = true;
    public bool NotifyOnLikes { get; init; } = false;
    public bool NotifyOnShares { get; init; } = false;
    public bool NotifyOnUpdates { get; init; } = true;
}

public record PostDto
{
    public Guid Id { get; init; }
    public Guid AuthorId { get; init; }
    public Guid? TenantId { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? MediaUrl { get; init; }
    public string? MediaType { get; init; }
    public string Visibility { get; init; } = "Public";
    public bool IsPinned { get; init; }
    public bool IsEdited { get; init; }
    public DateTime? EditedAt { get; init; }
    public int LikesCount { get; init; }
    public int CommentsCount { get; init; }
    public int SharesCount { get; init; }
    public int ViewsCount { get; init; }
    public Guid? ReplyToPostId { get; init; }
    public Guid? RepostOfPostId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record CommentDto
{
    public Guid Id { get; init; }
    public Guid PostId { get; init; }
    public Guid AuthorId { get; init; }
    public Guid? ParentCommentId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsEdited { get; init; }
    public DateTime? EditedAt { get; init; }
    public int LikesCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record TagDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = "general";
    public string? Color { get; init; }
    public int UsageCount { get; init; }
    public bool IsFeatured { get; init; }
}

public record StatisticsDto
{
    public Guid PostId { get; init; }
    public int ViewsCount { get; init; }
    public int UniqueViewersCount { get; init; }
    public int ExternalSharesCount { get; init; }
    public double AverageEngagementTime { get; init; }
    public double EngagementScore { get; init; }
    public double TrendingScore { get; init; }
    public DateTime LastCalculatedAt { get; init; }
}

public record FollowerDto
{
    public Guid PostId { get; init; }
    public Guid UserId { get; init; }
    public bool NotifyOnComments { get; init; }
    public bool NotifyOnLikes { get; init; }
    public bool NotifyOnShares { get; init; }
    public bool NotifyOnUpdates { get; init; }
    public DateTime CreatedAt { get; init; }
}

#endregion
