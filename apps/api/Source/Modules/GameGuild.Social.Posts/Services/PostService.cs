using GameGuild.Abstractions;
using GameGuild.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Implementation of the post service with full CRUD, statistics, and engagement tracking
/// </summary>
public class PostService : IPostService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PostService> _logger;

    private static class PostErrors
    {
        public static Error NotFound => Error.NotFound("Post.NotFound", "Post not found");
        public static Error CommentNotFound => Error.NotFound("Comment.NotFound", "Comment not found");
        public static Error ParentCommentNotFound => Error.NotFound("ParentComment.NotFound", "Parent comment not found");
        public static Error StatisticsNotFound => Error.NotFound("PostStatistics.NotFound", "Statistics not found for post");
        public static Error ViewNotFound => Error.NotFound("PostView.NotFound", "View not found");
        public static Error ReferenceNotFound => Error.NotFound("ContentReference.NotFound", "Content reference not found");
        public static Error NotFollowing => Error.NotFound("PostFollower.NotFound", "Not following this post");
    }

    public PostService(IApplicationDbContext context, ILogger<PostService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Basic CRUD Operations

    public async Task<Result<Post>> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        return post is null
            ? Result.Failure<Post>(PostErrors.NotFound)
            : Result.Success(post);
    }

    public async Task<Result<Post>> GetPostByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetPostByIdAsync(id, cancellationToken);
    }

    public async Task<Result<IEnumerable<Post>>> GetPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<Post>> CreatePostAsync(
        Guid authorId,
        string content,
        PostVisibility visibility = PostVisibility.Public,
        string? mediaUrl = null,
        MediaType? mediaType = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var post = Post.Create(authorId, content, visibility, tenantId);

        _context.Set<Post>().Add(post);

        var statistics = PostStatistics.Create(post.Id);
        _context.Set<PostStatistics>().Add(statistics);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created post {PostId} by author {AuthorId}", post.Id, authorId);

        return Result.Success(post);
    }

    public async Task<Result<Post>> UpdatePostAsync(Guid postId, string content, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<Post>(PostErrors.NotFound);

        post.Edit(content);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated post {PostId}", postId);

        return Result.Success(post);
    }

    public async Task<Result> DeletePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        post.Delete();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted post {PostId}", postId);

        return Result.Success();
    }

    public async Task<Result> RestorePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        post.Restore();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Restored post {PostId}", postId);

        return Result.Success();
    }

    #endregion

    #region Filtered Queries

    public async Task<Result<IEnumerable<Post>>> GetPostsByAuthorAsync(Guid authorId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => p.AuthorId == authorId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetPostsByVisibilityAsync(PostVisibility visibility, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => p.Visibility == visibility && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetPinnedPostsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Post>()
            .Where(p => p.IsPinned && !p.IsDeleted);

        if (tenantId.HasValue)
            query = query.Where(p => p.TenantId == tenantId.Value);

        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetPublicPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => p.Visibility == PostVisibility.Public && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetPostsByTenantAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => p.TenantId == tenantId && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    #endregion

    #region Search and Advanced Queries

    public async Task<Result<IEnumerable<Post>>> SearchPostsAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => !p.IsDeleted && p.Content.Contains(searchTerm))
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetPostsByTagsAsync(string[] tags, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var normalizedTags = tags.Select(t => t.ToLowerInvariant().Trim()).ToArray();

        var postIds = await _context.Set<PostTagAssignment>()
            .Join(_context.Set<PostTag>(),
                assignment => assignment.TagId,
                tag => tag.Id,
                (assignment, tag) => new { assignment.PostId, tag.Name })
            .Where(x => normalizedTags.Contains(x.Name))
            .Select(x => x.PostId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var posts = await _context.Set<Post>()
            .Where(p => postIds.Contains(p.Id) && !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    public async Task<Result<IEnumerable<Post>>> GetTrendingPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var postIds = await _context.Set<PostStatistics>()
            .OrderByDescending(s => s.TrendingScore)
            .Skip(skip)
            .Take(take)
            .Select(s => s.PostId)
            .ToListAsync(cancellationToken);

        var posts = await _context.Set<Post>()
            .Where(p => postIds.Contains(p.Id) && !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var orderedPosts = postIds
            .Select(id => posts.FirstOrDefault(p => p.Id == id))
            .Where(p => p is not null)
            .Cast<Post>()
            .ToList();

        return Result.Success<IEnumerable<Post>>(orderedPosts);
    }

    public async Task<Result<IEnumerable<Post>>> GetFeedPostsAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => !p.IsDeleted &&
                       (p.Visibility == PostVisibility.Public || p.AuthorId == userId))
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<Post>>(posts);
    }

    #endregion

    #region Post Interactions

    public async Task<Result<bool>> TogglePostLikeAsync(Guid postId, Guid userId, string reactionType = "like", CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<bool>(PostErrors.NotFound);

        var existingLike = await _context.Set<PostLike>()
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, cancellationToken);

        if (existingLike is not null)
        {
            _context.Set<PostLike>().Remove(existingLike);
            post.DecrementLikes();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(false);
        }
        else
        {
            var like = PostLike.Create(postId, userId, reactionType);
            _context.Set<PostLike>().Add(like);
            post.IncrementLikes();
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success(true);
        }
    }

    public async Task<Result<bool>> TogglePostPinAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<bool>(PostErrors.NotFound);

        if (post.IsPinned)
            post.Unpin();
        else
            post.Pin();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(post.IsPinned);
    }

    public async Task<Result> SharePostAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        post.IncrementShares();

        var statistics = await _context.Set<PostStatistics>()
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);

        statistics?.IncrementExternalShares();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<PostStatistics>> GetPostStatisticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var statistics = await _context.Set<PostStatistics>()
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);

        return statistics is null
            ? Result.Failure<PostStatistics>(PostErrors.StatisticsNotFound)
            : Result.Success(statistics);
    }

    public async Task<Result> RecordPostViewAsync(
        Guid postId,
        Guid? userId,
        string? ipAddress = null,
        string? userAgent = null,
        string? referrer = null,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        bool isUnique;
        if (userId.HasValue)
        {
            isUnique = !await _context.Set<PostView>()
                .AnyAsync(v => v.PostId == postId && v.UserId == userId.Value, cancellationToken);
        }
        else
        {
            isUnique = !await _context.Set<PostView>()
                .AnyAsync(v => v.PostId == postId && v.IpAddress == ipAddress, cancellationToken);
        }

        var view = PostView.Create(postId, userId, ipAddress, userAgent, referrer);
        _context.Set<PostView>().Add(view);

        post.IncrementViews();

        var statistics = await _context.Set<PostStatistics>()
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);

        statistics?.IncrementViews(isUnique);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateViewEngagementAsync(Guid viewId, int durationSeconds, bool engaged = false, CancellationToken cancellationToken = default)
    {
        var view = await _context.Set<PostView>()
            .FirstOrDefaultAsync(v => v.Id == viewId, cancellationToken);

        if (view is null)
            return Result.Failure(PostErrors.ViewNotFound);

        view.UpdateDuration(durationSeconds, engaged);

        var statistics = await _context.Set<PostStatistics>()
            .FirstOrDefaultAsync(s => s.PostId == view.PostId, cancellationToken);

        statistics?.UpdateEngagementTime(durationSeconds);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    #endregion

    #region Comments

    public async Task<Result<PostComment>> AddCommentAsync(Guid postId, Guid authorId, string content, Guid? parentCommentId = null, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostComment>(PostErrors.NotFound);

        if (parentCommentId.HasValue)
        {
            var parentExists = await _context.Set<PostComment>()
                .AnyAsync(c => c.Id == parentCommentId.Value && c.PostId == postId, cancellationToken);

            if (!parentExists)
                return Result.Failure<PostComment>(PostErrors.ParentCommentNotFound);
        }

        var comment = PostComment.Create(postId, authorId, content, parentCommentId);
        _context.Set<PostComment>().Add(comment);

        post.IncrementComments();

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Added comment {CommentId} to post {PostId}", comment.Id, postId);

        return Result.Success(comment);
    }

    public async Task<Result<PostComment>> UpdateCommentAsync(Guid commentId, string content, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Set<PostComment>()
            .FirstOrDefaultAsync(c => c.Id == commentId && !c.IsDeleted, cancellationToken);

        if (comment is null)
            return Result.Failure<PostComment>(PostErrors.CommentNotFound);

        comment.Edit(content);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(comment);
    }

    public async Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default)
    {
        var comment = await _context.Set<PostComment>()
            .FirstOrDefaultAsync(c => c.Id == commentId, cancellationToken);

        if (comment is null)
            return Result.Failure(PostErrors.CommentNotFound);

        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == comment.PostId, cancellationToken);

        comment.Delete();
        post?.DecrementComments();

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<PostComment>>> GetPostCommentsAsync(Guid postId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        var comments = await _context.Set<PostComment>()
            .Where(c => c.PostId == postId && !c.IsDeleted)
            .OrderBy(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<PostComment>>(comments);
    }

    #endregion

    #region Tags

    public async Task<Result> AddTagsToPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        var order = 0;
        foreach (var tagName in tagNames)
        {
            var tagResult = await GetOrCreateTagAsync(tagName, cancellationToken: cancellationToken);
            if (!tagResult.IsSuccess) continue;

            var tag = tagResult.Value!;

            var existingAssignment = await _context.Set<PostTagAssignment>()
                .AnyAsync(a => a.PostId == postId && a.TagId == tag.Id, cancellationToken);

            if (!existingAssignment)
            {
                var assignment = PostTagAssignment.Create(postId, tag.Id, order++);
                _context.Set<PostTagAssignment>().Add(assignment);
                tag.IncrementUsage();
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> RemoveTagsFromPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default)
    {
        var normalizedNames = tagNames.Select(t => t.ToLowerInvariant().Trim()).ToArray();

        var tags = await _context.Set<PostTag>()
            .Where(t => normalizedNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var tagIds = tags.Select(t => t.Id).ToList();

        var assignments = await _context.Set<PostTagAssignment>()
            .Where(a => a.PostId == postId && tagIds.Contains(a.TagId))
            .ToListAsync(cancellationToken);

        foreach (var assignment in assignments)
        {
            _context.Set<PostTagAssignment>().Remove(assignment);
            var tag = tags.FirstOrDefault(t => t.Id == assignment.TagId);
            tag?.DecrementUsage();
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<PostTag>>> GetPostTagsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var tagIds = await _context.Set<PostTagAssignment>()
            .Where(a => a.PostId == postId)
            .OrderBy(a => a.Order)
            .Select(a => a.TagId)
            .ToListAsync(cancellationToken);

        var tags = await _context.Set<PostTag>()
            .Where(t => tagIds.Contains(t.Id))
            .ToListAsync(cancellationToken);

        var orderedTags = tagIds
            .Select(id => tags.FirstOrDefault(t => t.Id == id))
            .Where(t => t is not null)
            .Cast<PostTag>()
            .ToList();

        return Result.Success<IEnumerable<PostTag>>(orderedTags);
    }

    public async Task<Result<PostTag>> GetOrCreateTagAsync(string name, string category = "general", CancellationToken cancellationToken = default)
    {
        var normalizedName = name.ToLowerInvariant().Trim();

        var existingTag = await _context.Set<PostTag>()
            .FirstOrDefaultAsync(t => t.Name == normalizedName, cancellationToken);

        if (existingTag is not null)
            return Result.Success(existingTag);

        var newTag = PostTag.Create(normalizedName, name, category);
        _context.Set<PostTag>().Add(newTag);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(newTag);
    }

    public async Task<Result<IEnumerable<PostTag>>> GetPopularTagsAsync(int count = 20, CancellationToken cancellationToken = default)
    {
        var tags = await _context.Set<PostTag>()
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.IsFeatured)
            .Take(count)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<PostTag>>(tags);
    }

    #endregion

    #region Content References

    public async Task<Result<PostContentReference>> AddContentReferenceAsync(
        Guid postId,
        Guid resourceId,
        string resourceType,
        string referenceType = "mention",
        string? context = null,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostContentReference>(PostErrors.NotFound);

        var order = await _context.Set<PostContentReference>()
            .CountAsync(r => r.PostId == postId, cancellationToken);

        var reference = PostContentReference.Create(postId, resourceId, resourceType, referenceType, context, order);
        _context.Set<PostContentReference>().Add(reference);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(reference);
    }

    public async Task<Result> RemoveContentReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default)
    {
        var reference = await _context.Set<PostContentReference>()
            .FirstOrDefaultAsync(r => r.Id == referenceId, cancellationToken);

        if (reference is null)
            return Result.Failure(PostErrors.ReferenceNotFound);

        _context.Set<PostContentReference>().Remove(reference);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<PostContentReference>>> GetPostContentReferencesAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var references = await _context.Set<PostContentReference>()
            .Where(r => r.PostId == postId)
            .OrderBy(r => r.Order)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<PostContentReference>>(references);
    }

    #endregion

    #region Post Following

    public async Task<Result<PostFollower>> FollowPostAsync(
        Guid postId,
        Guid userId,
        bool notifyOnComments = true,
        bool notifyOnLikes = false,
        bool notifyOnShares = false,
        bool notifyOnUpdates = true,
        CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId && !p.IsDeleted, cancellationToken);

        if (post is null)
            return Result.Failure<PostFollower>(PostErrors.NotFound);

        var existingFollow = await _context.Set<PostFollower>()
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

        if (existingFollow is not null)
            return Result.Success(existingFollow);

        var follower = PostFollower.Create(postId, userId, notifyOnComments, notifyOnLikes, notifyOnShares, notifyOnUpdates);
        _context.Set<PostFollower>().Add(follower);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success(follower);
    }

    public async Task<Result> UnfollowPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var follow = await _context.Set<PostFollower>()
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

        if (follow is null)
            return Result.Failure(PostErrors.NotFollowing);

        _context.Set<PostFollower>().Remove(follow);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result> UpdateFollowPreferencesAsync(
        Guid postId,
        Guid userId,
        bool? notifyOnComments = null,
        bool? notifyOnLikes = null,
        bool? notifyOnShares = null,
        bool? notifyOnUpdates = null,
        CancellationToken cancellationToken = default)
    {
        var follow = await _context.Set<PostFollower>()
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

        if (follow is null)
            return Result.Failure(PostErrors.NotFollowing);

        follow.UpdatePreferences(notifyOnComments, notifyOnLikes, notifyOnShares, notifyOnUpdates);
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<IEnumerable<PostFollower>>> GetPostFollowersAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var followers = await _context.Set<PostFollower>()
            .Where(f => f.PostId == postId)
            .ToListAsync(cancellationToken);

        return Result.Success<IEnumerable<PostFollower>>(followers);
    }

    public async Task<Result<bool>> IsFollowingPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default)
    {
        var isFollowing = await _context.Set<PostFollower>()
            .AnyAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

        return Result.Success(isFollowing);
    }

    #endregion

    #region Validation and Utilities

    public async Task<Result<bool>> CanUserPerformActionAsync(Guid postId, Guid userId, string action, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure<bool>(PostErrors.NotFound);

        if (post.AuthorId == userId)
            return Result.Success(true);

        return action.ToLowerInvariant() switch
        {
            "view" => Result.Success(post.Visibility == PostVisibility.Public || post.Visibility == PostVisibility.Unlisted),
            "like" or "comment" => Result.Success(post.Visibility != PostVisibility.Private),
            "edit" or "delete" or "pin" => Result.Success(false),
            _ => Result.Success(false)
        };
    }

    public async Task<Result> RecalculateStatisticsAsync(Guid postId, CancellationToken cancellationToken = default)
    {
        var post = await _context.Set<Post>()
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post is null)
            return Result.Failure(PostErrors.NotFound);

        var statistics = await _context.Set<PostStatistics>()
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);

        if (statistics is null)
        {
            statistics = PostStatistics.Create(postId);
            _context.Set<PostStatistics>().Add(statistics);
        }

        var hoursOld = (int)(DateTime.UtcNow - post.CreatedAt).TotalHours;
        statistics.RecalculateScores(post.LikesCount, post.CommentsCount, post.SharesCount, hoursOld);

        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    public async Task<Result<int>> RecalculateAllTrendingScoresAsync(CancellationToken cancellationToken = default)
    {
        var posts = await _context.Set<Post>()
            .Where(p => !p.IsDeleted)
            .ToListAsync(cancellationToken);

        var statistics = await _context.Set<PostStatistics>()
            .ToListAsync(cancellationToken);

        var statsDict = statistics.ToDictionary(s => s.PostId);
        var count = 0;

        foreach (var post in posts)
        {
            if (!statsDict.TryGetValue(post.Id, out var stats))
            {
                stats = PostStatistics.Create(post.Id);
                _context.Set<PostStatistics>().Add(stats);
                statsDict[post.Id] = stats;
            }

            var hoursOld = (int)(DateTime.UtcNow - post.CreatedAt).TotalHours;
            stats.RecalculateScores(post.LikesCount, post.CommentsCount, post.SharesCount, hoursOld);
            count++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Recalculated trending scores for {Count} posts", count);

        return Result.Success(count);
    }

    #endregion
}
