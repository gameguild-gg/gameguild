using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Contents;
using GameGuild.Modules.Posts.Models;


namespace GameGuild.Modules.Posts.GraphQL;

/// <summary>
/// GraphQL queries for Posts
/// </summary>
[ExtendObjectType<Query>]
public class PostQueries {
  /// <summary>
  /// Get paginated list of posts with filtering and sorting options
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPosts(
    [Service] ApplicationDbContext context,
    string? postType = null,
    Guid? userId = null,
    bool? isPinned = null,
    string? searchTerm = null,
    Guid? tenantId = null
  ) {
    var query = context.Posts.AsQueryable();

    if (!string.IsNullOrEmpty(postType)) { query = query.Where(p => p.PostType == postType); }

    if (userId.HasValue) { query = query.Where(p => p.AuthorId == userId.Value); }

    if (isPinned.HasValue) { query = query.Where(p => p.IsPinned == isPinned.Value); }

    if (!string.IsNullOrEmpty(searchTerm)) {
      query = query.Where(p => p.Title.Contains(searchTerm) ||
                               (p.Description != null && p.Description.Contains(searchTerm))
      );
    }

    return query.OrderByDescending(p => p.CreatedAt);
  }

  /// <summary>
  /// Get a specific post by ID
  /// </summary>
  public async Task<Post?> GetPost(
    Guid id,
    [Service] ApplicationDbContext context,
    CancellationToken cancellationToken = default
  ) {
    return await context.Posts
                        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
  }

  /// <summary>
  /// Get posts by a specific user
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPostsByUser(
    Guid userId,
    [Service] ApplicationDbContext context,
    string? postType = null,
    bool? isPinned = null
  ) {
    var query = context.Posts
                       .Where(p => p.AuthorId == userId);

    if (!string.IsNullOrEmpty(postType)) { query = query.Where(p => p.PostType == postType); }

    if (isPinned.HasValue) { query = query.Where(p => p.IsPinned == isPinned.Value); }

    return query;
  }

  /// <summary>
  /// Get system-generated posts (announcements, welcome messages, etc.)
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetSystemPosts([Service] ApplicationDbContext context) { return context.Posts.Where(p => p.IsSystemGenerated); }

  /// <summary>
  /// Get pinned posts across all tenants (public feed)
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPinnedPosts([Service] ApplicationDbContext context) { return context.Posts.Where(p => p.IsPinned && p.Visibility == AccessLevel.Public); }

  /// <summary>
  /// Get trending posts based on engagement metrics
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetTrendingPosts([Service] ApplicationDbContext context) {
    return context.Posts
                  .Where(p => p.Visibility == AccessLevel.Public)
                  .OrderByDescending(p => p.LikesCount + p.CommentsCount + p.SharesCount)
                  .ThenByDescending(p => p.CreatedAt);
  }

  /// <summary>
  /// Search posts by title and description
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> SearchPosts(
    string searchTerm,
    [Service] ApplicationDbContext context
  ) {
    return context.Posts
                  .Where(p => p.Title.Contains(searchTerm) ||
                              (p.Description != null && p.Description.Contains(searchTerm))
                  )
                  .OrderByDescending(p => p.CreatedAt);
  }

  /// <summary>
  /// Get posts by content status
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPostsByStatus(
    ContentStatus status,
    [Service] ApplicationDbContext context
  ) {
    return context.Posts.Where(p => p.Status == status);
  }

  /// <summary>
  /// Get posts by visibility level
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPostsByVisibility(
    AccessLevel visibility,
    [Service] ApplicationDbContext context
  ) {
    return context.Posts.Where(p => p.Visibility == visibility);
  }

  /// <summary>
  /// Get posts by multiple tags
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPostsByTags(
    string[] tags,
    [Service] ApplicationDbContext context
  ) {
    return context.Posts
                  .Where(p => p.Tags.Any(pt => tags.Contains(pt.Tag.Name)))
                  .OrderByDescending(p => p.CreatedAt);
  }

  /// <summary>
  /// Get user's feed posts (posts from followed users and interests)
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetFeedPosts(
    Guid userId,
    [Service] ApplicationDbContext context
  ) {
    // For now, return public posts - in a full implementation this would include:
    // - Posts from users the current user follows
    // - Posts with tags the user is interested in
    // - Posts from communities the user is part of
    return context.Posts
                  .Where(p => p.Visibility == AccessLevel.Public &&
                              p.Status == ContentStatus.Published
                  )
                  .OrderByDescending(p => p.CreatedAt);
  }

  /// <summary>
  /// Get posts from a specific tenant
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetPostsByTenant(
    Guid tenantId,
    [Service] ApplicationDbContext context
  ) {
    return context.Posts.Where(p => p.Tenant != null && p.Tenant.Id == tenantId);
  }

  /// <summary>
  /// Get deleted posts (admin only)
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<Post> GetDeletedPosts([Service] ApplicationDbContext context) {
    return context.Posts
                  .IgnoreQueryFilters()
                  .Where(p => p.DeletedAt != null);
  }

  /// <summary>
  /// Get post statistics
  /// </summary>
  public async Task<PostStatistics?> GetPostStatistics(
    Guid postId,
    [Service] ApplicationDbContext context,
    CancellationToken cancellationToken = default
  ) {
    return await context.PostStatistics
                        .FirstOrDefaultAsync(s => s.PostId == postId, cancellationToken);
  }

  /// <summary>
  /// Get post with full details including all relationships
  /// </summary>
  public async Task<Post?> GetPostWithDetails(
    Guid id,
    [Service] ApplicationDbContext context,
    CancellationToken cancellationToken = default
  ) {
    return await context.Posts
                        .Include(p => p.Author)
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
                        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
  }

  /// <summary>
  /// Get all available post tags
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<PostTag> GetPostTags([Service] ApplicationDbContext context) { return context.PostTags.OrderBy(t => t.Name); }

  /// <summary>
  /// Get popular/featured tags
  /// </summary>
  [UsePaging]
  [UseFiltering]
  [UseSorting]
  public IQueryable<PostTag> GetFeaturedTags([Service] ApplicationDbContext context) {
    return context.PostTags
                  .Where(t => t.IsFeatured || t.UsageCount > 10)
                  .OrderByDescending(t => t.UsageCount);
  }
}
