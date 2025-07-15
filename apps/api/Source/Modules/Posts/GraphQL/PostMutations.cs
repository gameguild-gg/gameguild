using GameGuild.Modules.Contents;
using GameGuild.Database;
using GameGuild.Common;
using MediatR;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Posts;

/// <summary>
/// GraphQL mutations for Posts
/// </summary>
[ExtendObjectType<Mutation>]
public class PostMutations
{
    /// <summary>
    /// Create a new post
    /// </summary>
    public async Task<Post> CreatePost(
        CreatePostInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new CreatePostCommand
        {
            Title = input.Title,
            Description = input.Description,
            PostType = input.PostType ?? "general",
            AuthorId = input.UserId ?? Guid.NewGuid(), // TODO: Get from authenticated user
            Visibility = input.Visibility ?? AccessLevel.Public,
            IsSystemGenerated = input.IsPinned ?? false,
            RichContent = input.RichContent,
            ContentReferences = input.Tags?.Select(Guid.Parse).ToList() // Convert tags to GUIDs if needed
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Update an existing post
    /// </summary>
    public async Task<Post> UpdatePost(
        UpdatePostInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdatePostCommand
        {
            PostId = input.PostId,
            Title = input.Title,
            Description = input.Description,
            Visibility = input.Visibility,
            RichContent = input.RichContent
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Delete a post
    /// </summary>
    public async Task<bool> DeletePost(
        Guid postId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new DeletePostCommand
        {
            PostId = postId
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Like or unlike a post
    /// </summary>
    public async Task<bool> TogglePostLike(
        Guid postId,
        Guid userId,
        [Service] IMediator mediator,
        string reactionType = "like",
        CancellationToken cancellationToken = default)
    {
        var command = new TogglePostLikeCommand
        {
            PostId = postId,
            UserId = userId,
            ReactionType = reactionType
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Pin or unpin a post
    /// </summary>
    public async Task<Post> TogglePostPin(
        Guid postId,
        Guid userId,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new TogglePostPinCommand
        {
            PostId = postId,
            UserId = userId
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Share a post (increment share count)
    /// </summary>
    public async Task<bool> SharePost(
        Guid postId,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post == null)
        {
            throw new GraphQLException("Post not found");
        }

        post.SharesCount++;
        post.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Add a comment to a post
    /// </summary>
    public async Task<PostComment> AddComment(
        AddCommentInput input,
        [Service] IMediator mediator,
        CancellationToken cancellationToken = default)
    {
        var command = new AddCommentCommand
        {
            PostId = input.PostId,
            AuthorId = input.UserId,
            Content = input.Content,
            ParentCommentId = input.ParentCommentId
        };

        var result = await mediator.Send(command, cancellationToken);
        
        if (!result.IsSuccess)
        {
            throw new GraphQLException(result.Error.Description);
        }

        return result.Value;
    }

    /// <summary>
    /// Follow or unfollow a post for notifications
    /// </summary>
    public async Task<bool> TogglePostFollow(
        Guid postId,
        Guid userId,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var existingFollow = await context.PostFollowers
            .FirstOrDefaultAsync(f => f.PostId == postId && f.UserId == userId, cancellationToken);

        if (existingFollow != null)
        {
            // Unfollow
            context.PostFollowers.Remove(existingFollow);
            await context.SaveChangesAsync(cancellationToken);
            return false;
        }
        else
        {
            // Follow
            var follow = new PostFollower
            {
                PostId = postId,
                UserId = userId,
                NotifyOnComments = true,
                NotifyOnLikes = false,
                NotifyOnShares = false,
                NotifyOnUpdates = true
            };
            
            context.PostFollowers.Add(follow);
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }

    /// <summary>
    /// Create a tag and assign it to posts
    /// </summary>
    public async Task<PostTag> CreateTag(
        CreateTagInput input,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        // Check if tag already exists
        var existingTag = await context.PostTags
            .FirstOrDefaultAsync(t => t.Name == input.Name, cancellationToken);
            
        if (existingTag != null)
        {
            throw new GraphQLException($"Tag '{input.Name}' already exists");
        }

        var tag = new PostTag
        {
            Name = input.Name,
            DisplayName = input.DisplayName ?? input.Name,
            Description = input.Description,
            Category = input.Category ?? "general",
            Color = input.Color,
            IsFeatured = input.IsFeatured ?? false
        };

        context.PostTags.Add(tag);
        await context.SaveChangesAsync(cancellationToken);
        
        return tag;
    }

    /// <summary>
    /// Assign tags to a post
    /// </summary>
    public async Task<Post> AssignTagsToPost(
        Guid postId,
        string[] tagNames,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .Include(p => p.Tags)
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            
        if (post == null)
        {
            throw new GraphQLException("Post not found");
        }

        // Remove existing tag assignments
        context.PostTagAssignments.RemoveRange(post.Tags);

        // Add new tag assignments
        var tags = await context.PostTags
            .Where(t => tagNames.Contains(t.Name))
            .ToListAsync(cancellationToken);

        var order = 0;
        foreach (var tag in tags)
        {
            var assignment = new PostTagAssignment
            {
                PostId = postId,
                TagId = tag.Id,
                Order = order++
            };
            
            context.PostTagAssignments.Add(assignment);
            
            // Update tag usage count
            tag.UsageCount++;
        }

        post.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        return post;
    }

    /// <summary>
    /// Record a post view for analytics
    /// </summary>
    public async Task<bool> RecordPostView(
        Guid postId,
        Guid? userId,
        string? ipAddress,
        string? userAgent,
        string? referrer,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var view = new PostView
        {
            PostId = postId,
            UserId = userId,
            ViewedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Referrer = referrer
        };

        context.PostViews.Add(view);

        // Update post statistics
        var statistics = await context.PostStatistics
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);
            
        if (statistics != null)
        {
            statistics.ViewsCount++;
            if (userId.HasValue)
            {
                statistics.UniqueViewersCount++;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Restore a deleted post
    /// </summary>
    public async Task<Post> RestorePost(
        Guid postId,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var post = await context.Posts
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);
            
        if (post == null)
        {
            throw new GraphQLException("Post not found");
        }

        post.DeletedAt = null;
        post.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync(cancellationToken);
        return post;
    }

    /// <summary>
    /// Bulk delete posts
    /// </summary>
    public async Task<bool> BulkDeletePosts(
        Guid[] postIds,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var posts = await context.Posts
            .Where(p => postIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        foreach (var post in posts)
        {
            post.DeletedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Update post statistics (admin operation)
    /// </summary>
    public async Task<PostStatistics> UpdatePostStatistics(
        Guid postId,
        [Service] ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        var statistics = await context.PostStatistics
            .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);
            
        if (statistics == null)
        {
            statistics = new PostStatistics { PostId = postId };
            context.PostStatistics.Add(statistics);
        }

        // Calculate engagement metrics
        var post = await context.Posts
            .Include(p => p.Views)
            .Include(p => p.Likes)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == postId, cancellationToken);

        if (post != null)
        {
            statistics.ViewsCount = post.Views.Count;
            statistics.UniqueViewersCount = post.Views.Where(v => v.UserId.HasValue).DistinctBy(v => v.UserId).Count();
            statistics.AverageEngagementTime = post.Views.Where(v => v.DurationSeconds > 0).DefaultIfEmpty().Average(v => v.DurationSeconds);
            
            // Calculate engagement score (likes + comments + shares weighted)
            statistics.EngagementScore = (post.LikesCount * 1.0) + (post.CommentsCount * 2.0) + (post.SharesCount * 3.0);
            
            // Calculate trending score (engagement over time)
            var hoursSinceCreated = (DateTime.UtcNow - post.CreatedAt).TotalHours;
            statistics.TrendingScore = hoursSinceCreated > 0 ? statistics.EngagementScore / hoursSinceCreated : 0;
        }

        statistics.LastCalculatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
        
        return statistics;
    }
}

/// <summary>
/// Input type for creating a new post
/// </summary>
public record CreatePostInput(
    string Title
)
{
    public string? Description { get; init; }
    public string? PostType { get; init; }
    public AccessLevel? Visibility { get; init; }
    public Guid? UserId { get; init; }
    public bool? IsPinned { get; init; }
    public string? RichContent { get; init; }
    public List<string>? Tags { get; init; }
}

/// <summary>
/// Input type for updating an existing post
/// </summary>
public record UpdatePostInput(
    Guid PostId
)
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? PostType { get; init; }
    public AccessLevel? Visibility { get; init; }
    public bool? IsPinned { get; init; }
    public string? RichContent { get; init; }
    public List<string>? Tags { get; init; }
}
