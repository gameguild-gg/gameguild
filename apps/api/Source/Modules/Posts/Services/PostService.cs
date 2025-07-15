using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Posts.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Posts.Services;

/// <summary>
/// Service implementation for Post business logic
/// Provides comprehensive post management capabilities matching ProjectService features
/// </summary>
public class PostService(ApplicationDbContext context, ILogger<PostService> logger) : IPostService
{
    #region Basic CRUD Operations

    public async Task<Post?> GetPostByIdAsync(Guid id)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Post?> GetPostByIdWithDetailsAsync(Guid id)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Tenant)
            .Include(p => p.Statistics)
            .Include(p => p.Comments.Where(c => c.DeletedAt == null))
                .ThenInclude(c => c.Author)
            .Include(p => p.Likes)
                .ThenInclude(l => l.User)
            .Include(p => p.Tags)
                .ThenInclude(t => t.Tag)
            .Include(p => p.Followers)
                .ThenInclude(f => f.User)
            .Include(p => p.ContentReferences)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Post?> GetPostBySlugAsync(string slug)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null)
            .FirstOrDefaultAsync(p => p.Slug == slug);
    }

    public async Task<IEnumerable<Post>> GetPostsAsync(int skip = 0, int take = 50)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetAllPostsAsync()
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Post> CreatePostAsync(Post post)
    {
        logger.LogInformation("Creating post: {Title} by {AuthorId}", post.Title, post.AuthorId);

        // Generate slug if not provided
        if (string.IsNullOrEmpty(post.Slug))
        {
            post.Slug = await GenerateUniqueSlugAsync(post.Title, post.Tenant?.Id);
        }

        // Set timestamps
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = DateTime.UtcNow;

        context.Posts.Add(post);
        await context.SaveChangesAsync();

        // Create initial statistics
        var statistics = new PostStatistics
        {
            PostId = post.Id,
            LastCalculatedAt = DateTime.UtcNow
        };
        context.PostStatistics.Add(statistics);
        await context.SaveChangesAsync();

        logger.LogInformation("Post created successfully: {PostId}", post.Id);
        return post;
    }

    public async Task<Post> UpdatePostAsync(Post post)
    {
        logger.LogInformation("Updating post: {PostId}", post.Id);

        var existingPost = await context.Posts.FindAsync(post.Id);
        if (existingPost == null)
            throw new InvalidOperationException($"Post with ID {post.Id} not found");

        // Update properties
        existingPost.Title = post.Title;
        existingPost.Description = post.Description;
        existingPost.Visibility = post.Visibility;
        existingPost.Status = post.Status;
        existingPost.RichContent = post.RichContent;
        existingPost.IsPinned = post.IsPinned;
        existingPost.UpdatedAt = DateTime.UtcNow;

        // Update slug if title changed
        if (!string.IsNullOrEmpty(post.Title) && existingPost.Slug != Post.GenerateSlug(post.Title))
        {
            existingPost.Slug = await GenerateUniqueSlugAsync(post.Title, existingPost.Tenant?.Id);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("Post updated successfully: {PostId}", post.Id);
        return existingPost;
    }

    public async Task<bool> DeletePostAsync(Guid id)
    {
        logger.LogInformation("Deleting post: {PostId}", id);

        var post = await context.Posts.FindAsync(id);
        if (post == null) return false;

        post.DeletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        logger.LogInformation("Post deleted successfully: {PostId}", id);
        return true;
    }

    public async Task<bool> RestorePostAsync(Guid id)
    {
        logger.LogInformation("Restoring post: {PostId}", id);

        var post = await context.Posts.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (post == null) return false;

        post.DeletedAt = null;
        post.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        logger.LogInformation("Post restored successfully: {PostId}", id);
        return true;
    }

    #endregion

    #region Filtered Queries

    public async Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.AuthorId == authorId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByTypeAsync(string postType)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.PostType == postType && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByStatusAsync(ContentStatus status)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.Status == status && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByVisibilityAsync(AccessLevel visibility)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.Visibility == visibility && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetSystemPostsAsync()
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.IsSystemGenerated && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetUserPostsAsync()
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => !p.IsSystemGenerated && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPinnedPostsAsync()
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.IsPinned && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByTenantAsync(Guid tenantId)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.Tenant != null && p.Tenant.Id == tenantId && p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPublicPostsAsync()
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.Visibility == AccessLevel.Public && 
                       p.Status == ContentStatus.Published && 
                       p.DeletedAt == null)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetDeletedPostsAsync()
    {
        return await context.Posts
            .IgnoreQueryFilters()
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt != null)
            .OrderByDescending(p => p.DeletedAt)
            .ToListAsync();
    }

    #endregion

    #region Search and Advanced Queries

    public async Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm, int skip = 0, int take = 50)
    {
        var normalizedTerm = searchTerm.ToLower();
        
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null &&
                       (p.Title.ToLower().Contains(normalizedTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(normalizedTerm))))
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetPostsByTagsAsync(string[] tags)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Include(p => p.Tags)
                .ThenInclude(t => t.Tag)
            .Where(p => p.DeletedAt == null &&
                       p.Tags.Any(pt => tags.Contains(pt.Tag.Name)))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetTrendingPostsAsync(int skip = 0, int take = 50)
    {
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null && p.Visibility == AccessLevel.Public)
            .OrderByDescending(p => p.Statistics != null ? p.Statistics.TrendingScore : 0)
            .ThenByDescending(p => p.LikesCount + p.CommentsCount + p.SharesCount)
            .ThenByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IEnumerable<Post>> GetFeedPostsAsync(Guid userId, int skip = 0, int take = 50)
    {
        // Get posts from users the current user follows (this would need a user following system)
        // For now, return recent public posts
        return await context.Posts
            .Include(p => p.Author)
            .Include(p => p.Statistics)
            .Where(p => p.DeletedAt == null && 
                       p.Visibility == AccessLevel.Public &&
                       p.Status == ContentStatus.Published)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    #endregion

    #region Post Interactions

    public async Task<bool> TogglePostLikeAsync(Guid postId, Guid userId, string reactionType = "like")
    {
        var existingLike = await context.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId && l.ReactionType == reactionType);

        if (existingLike != null)
        {
            // Remove like
            context.PostLikes.Remove(existingLike);
            await UpdatePostLikesCount(postId, -1);
        }
        else
        {
            // Add like
            var like = new PostLike
            {
                PostId = postId,
                UserId = userId,
                ReactionType = reactionType,
                CreatedAt = DateTime.UtcNow
            };
            context.PostLikes.Add(like);
            await UpdatePostLikesCount(postId, 1);
        }

        await context.SaveChangesAsync();
        return existingLike == null; // Returns true if like was added, false if removed
    }

    public async Task<bool> TogglePostPinAsync(Guid postId, Guid userId)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post == null) return false;

        // TODO: Check if user has permission to pin posts
        
        post.IsPinned = !post.IsPinned;
        post.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return post.IsPinned;
    }

    public async Task<bool> SharePostAsync(Guid postId)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post == null) return false;

        post.SharesCount++;
        post.UpdatedAt = DateTime.UtcNow;
        
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<PostStatistics?> GetPostStatisticsAsync(Guid postId)
    {
        return await context.PostStatistics
            .FirstOrDefaultAsync(s => s.PostId == postId);
    }

    #endregion

    #region Validation and Utilities

    public async Task<bool> CanUserPerformActionAsync(Guid postId, Guid userId, string action)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post == null) return false;

        // Basic permission checks
        return action.ToLower() switch
        {
            "edit" or "delete" => post.AuthorId == userId, // Only author can edit/delete
            "like" or "comment" or "share" => true, // Anyone can interact (for now)
            "pin" => post.AuthorId == userId, // Only author can pin (TODO: add admin check)
            _ => false
        };
    }

    public async Task<string> GenerateUniqueSlugAsync(string title, Guid? tenantId = null)
    {
        var baseSlug = Post.GenerateSlug(title);
        var slug = baseSlug;
        var counter = 1;

        while (await context.Posts.AnyAsync(p => p.Slug == slug && 
                                                (tenantId == null || p.Tenant!.Id == tenantId)))
        {
            slug = $"{baseSlug}-{counter}";
            counter++;
        }

        return slug;
    }

    #endregion

    #region Private Helper Methods

    private async Task UpdatePostLikesCount(Guid postId, int change)
    {
        var post = await context.Posts.FindAsync(postId);
        if (post != null)
        {
            post.LikesCount = Math.Max(0, post.LikesCount + change);
            post.UpdatedAt = DateTime.UtcNow;
        }
    }

    #endregion
}
