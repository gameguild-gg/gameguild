using GameGuild.Common;
using GameGuild.Modules.Contents;

namespace GameGuild.Modules.Posts;

/// <summary>
/// Service interface for Post business logic operations
/// Provides comprehensive post management capabilities similar to ProjectService
/// </summary>
public interface IPostService
{
    #region Basic CRUD Operations

    /// <summary>
    /// Gets a post by ID with basic includes
    /// </summary>
    Task<Post?> GetPostByIdAsync(Guid id);

    /// <summary>
    /// Gets a post by ID with full details and relationships
    /// </summary>
    Task<Post?> GetPostByIdWithDetailsAsync(Guid id);

    /// <summary>
    /// Gets a post by slug
    /// </summary>
    Task<Post?> GetPostBySlugAsync(string slug);

    /// <summary>
    /// Gets paginated posts
    /// </summary>
    Task<IEnumerable<Post>> GetPostsAsync(int skip = 0, int take = 50);

    /// <summary>
    /// Gets all posts (use with caution)
    /// </summary>
    Task<IEnumerable<Post>> GetAllPostsAsync();

    /// <summary>
    /// Creates a new post
    /// </summary>
    Task<Post> CreatePostAsync(Post post);

    /// <summary>
    /// Updates an existing post
    /// </summary>
    Task<Post> UpdatePostAsync(Post post);

    /// <summary>
    /// Soft deletes a post
    /// </summary>
    Task<bool> DeletePostAsync(Guid id);

    /// <summary>
    /// Restores a soft-deleted post
    /// </summary>
    Task<bool> RestorePostAsync(Guid id);

    #endregion

    #region Filtered Queries

    /// <summary>
    /// Gets posts by author ID
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByAuthorAsync(Guid authorId);

    /// <summary>
    /// Gets posts by post type
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByTypeAsync(string postType);

    /// <summary>
    /// Gets posts by content status
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByStatusAsync(ContentStatus status);

    /// <summary>
    /// Gets posts by visibility level
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByVisibilityAsync(AccessLevel visibility);

    /// <summary>
    /// Gets system-generated posts
    /// </summary>
    Task<IEnumerable<Post>> GetSystemPostsAsync();

    /// <summary>
    /// Gets user-created posts
    /// </summary>
    Task<IEnumerable<Post>> GetUserPostsAsync();

    /// <summary>
    /// Gets pinned posts
    /// </summary>
    Task<IEnumerable<Post>> GetPinnedPostsAsync();

    /// <summary>
    /// Gets posts by tenant ID
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByTenantAsync(Guid tenantId);

    /// <summary>
    /// Gets public posts (for public API access)
    /// </summary>
    Task<IEnumerable<Post>> GetPublicPostsAsync();

    /// <summary>
    /// Gets deleted posts (for admin/restore purposes)
    /// </summary>
    Task<IEnumerable<Post>> GetDeletedPostsAsync();

    #endregion

    #region Search and Advanced Queries

    /// <summary>
    /// Searches posts by title and description
    /// </summary>
    Task<IEnumerable<Post>> SearchPostsAsync(string searchTerm, int skip = 0, int take = 50);

    /// <summary>
    /// Gets posts with specific tags
    /// </summary>
    Task<IEnumerable<Post>> GetPostsByTagsAsync(string[] tags);

    /// <summary>
    /// Gets trending posts (by likes, comments, shares)
    /// </summary>
    Task<IEnumerable<Post>> GetTrendingPostsAsync(int skip = 0, int take = 50);

    /// <summary>
    /// Gets recent posts from followed users (for feed functionality)
    /// </summary>
    Task<IEnumerable<Post>> GetFeedPostsAsync(Guid userId, int skip = 0, int take = 50);

    #endregion

    #region Post Interactions

    /// <summary>
    /// Toggles like on a post
    /// </summary>
    Task<bool> TogglePostLikeAsync(Guid postId, Guid userId, string reactionType = "like");

    /// <summary>
    /// Pins or unpins a post
    /// </summary>
    Task<bool> TogglePostPinAsync(Guid postId, Guid userId);

    /// <summary>
    /// Increments share count
    /// </summary>
    Task<bool> SharePostAsync(Guid postId);

    /// <summary>
    /// Gets post statistics
    /// </summary>
    Task<PostStatistics?> GetPostStatisticsAsync(Guid postId);

    #endregion

    #region Validation and Utilities

    /// <summary>
    /// Validates if user can perform action on post
    /// </summary>
    Task<bool> CanUserPerformActionAsync(Guid postId, Guid userId, string action);

    /// <summary>
    /// Generates unique slug for post
    /// </summary>
    Task<string> GenerateUniqueSlugAsync(string title, Guid? tenantId = null);

    #endregion
}
