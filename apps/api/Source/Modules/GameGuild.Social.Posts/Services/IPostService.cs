using GameGuild.Models;

namespace GameGuild.Social.Posts.Services;

/// <summary>
/// Service interface for post management operations
/// </summary>
public interface IPostService
{
    #region Basic CRUD Operations

    /// <summary>Gets a post by its ID</summary>
    Task<Result<Post>> GetPostByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets a post by ID with all related data (statistics, comments, likes)</summary>
    Task<Result<Post>> GetPostByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Gets paginated posts</summary>
    Task<Result<IEnumerable<Post>>> GetPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Creates a new post</summary>
    Task<Result<Post>> CreatePostAsync(
        Guid authorId,
        string content,
        PostVisibility visibility = PostVisibility.Public,
        string? mediaUrl = null,
        MediaType? mediaType = null,
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates an existing post</summary>
    Task<Result<Post>> UpdatePostAsync(Guid postId, string content, CancellationToken cancellationToken = default);

    /// <summary>Deletes a post (soft delete)</summary>
    Task<Result> DeletePostAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Restores a soft-deleted post</summary>
    Task<Result> RestorePostAsync(Guid postId, CancellationToken cancellationToken = default);

    #endregion

    #region Filtered Queries

    /// <summary>Gets posts by a specific author</summary>
    Task<Result<IEnumerable<Post>>> GetPostsByAuthorAsync(Guid authorId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets posts by visibility level</summary>
    Task<Result<IEnumerable<Post>>> GetPostsByVisibilityAsync(PostVisibility visibility, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets pinned posts</summary>
    Task<Result<IEnumerable<Post>>> GetPinnedPostsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>Gets public posts</summary>
    Task<Result<IEnumerable<Post>>> GetPublicPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets posts by tenant</summary>
    Task<Result<IEnumerable<Post>>> GetPostsByTenantAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    #endregion

    #region Search and Advanced Queries

    /// <summary>Searches posts by content</summary>
    Task<Result<IEnumerable<Post>>> SearchPostsAsync(string searchTerm, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets posts by tags</summary>
    Task<Result<IEnumerable<Post>>> GetPostsByTagsAsync(string[] tags, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets trending posts based on engagement score</summary>
    Task<Result<IEnumerable<Post>>> GetTrendingPostsAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>Gets personalized feed for a user</summary>
    Task<Result<IEnumerable<Post>>> GetFeedPostsAsync(Guid userId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    #endregion

    #region Post Interactions

    /// <summary>Toggles a like on a post</summary>
    Task<Result<bool>> TogglePostLikeAsync(Guid postId, Guid userId, string reactionType = "like", CancellationToken cancellationToken = default);

    /// <summary>Toggles pin status on a post</summary>
    Task<Result<bool>> TogglePostPinAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Records a share of the post</summary>
    Task<Result> SharePostAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Gets statistics for a post</summary>
    Task<Result<PostStatistics>> GetPostStatisticsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Records a view of the post</summary>
    Task<Result> RecordPostViewAsync(
        Guid postId,
        Guid? userId,
        string? ipAddress = null,
        string? userAgent = null,
        string? referrer = null,
        CancellationToken cancellationToken = default);

    /// <summary>Updates view engagement (duration, interaction)</summary>
    Task<Result> UpdateViewEngagementAsync(Guid viewId, int durationSeconds, bool engaged = false, CancellationToken cancellationToken = default);

    #endregion

    #region Comments

    /// <summary>Adds a comment to a post</summary>
    Task<Result<PostComment>> AddCommentAsync(Guid postId, Guid authorId, string content, Guid? parentCommentId = null, CancellationToken cancellationToken = default);

    /// <summary>Updates a comment</summary>
    Task<Result<PostComment>> UpdateCommentAsync(Guid commentId, string content, CancellationToken cancellationToken = default);

    /// <summary>Deletes a comment</summary>
    Task<Result> DeleteCommentAsync(Guid commentId, CancellationToken cancellationToken = default);

    /// <summary>Gets comments for a post</summary>
    Task<Result<IEnumerable<PostComment>>> GetPostCommentsAsync(Guid postId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    #endregion

    #region Tags

    /// <summary>Adds tags to a post</summary>
    Task<Result> AddTagsToPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default);

    /// <summary>Removes tags from a post</summary>
    Task<Result> RemoveTagsFromPostAsync(Guid postId, string[] tagNames, CancellationToken cancellationToken = default);

    /// <summary>Gets all tags for a post</summary>
    Task<Result<IEnumerable<PostTag>>> GetPostTagsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Gets or creates a tag by name</summary>
    Task<Result<PostTag>> GetOrCreateTagAsync(string name, string category = "general", CancellationToken cancellationToken = default);

    /// <summary>Gets popular tags</summary>
    Task<Result<IEnumerable<PostTag>>> GetPopularTagsAsync(int count = 20, CancellationToken cancellationToken = default);

    #endregion

    #region Content References

    /// <summary>Adds a content reference to a post</summary>
    Task<Result<PostContentReference>> AddContentReferenceAsync(
        Guid postId,
        Guid resourceId,
        string resourceType,
        string referenceType = "mention",
        string? context = null,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a content reference from a post</summary>
    Task<Result> RemoveContentReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);

    /// <summary>Gets content references for a post</summary>
    Task<Result<IEnumerable<PostContentReference>>> GetPostContentReferencesAsync(Guid postId, CancellationToken cancellationToken = default);

    #endregion

    #region Post Following

    /// <summary>Follows a post for notifications</summary>
    Task<Result<PostFollower>> FollowPostAsync(
        Guid postId,
        Guid userId,
        bool notifyOnComments = true,
        bool notifyOnLikes = false,
        bool notifyOnShares = false,
        bool notifyOnUpdates = true,
        CancellationToken cancellationToken = default);

    /// <summary>Unfollows a post</summary>
    Task<Result> UnfollowPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Updates notification preferences for a post follow</summary>
    Task<Result> UpdateFollowPreferencesAsync(
        Guid postId,
        Guid userId,
        bool? notifyOnComments = null,
        bool? notifyOnLikes = null,
        bool? notifyOnShares = null,
        bool? notifyOnUpdates = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets followers for a post</summary>
    Task<Result<IEnumerable<PostFollower>>> GetPostFollowersAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Checks if user is following a post</summary>
    Task<Result<bool>> IsFollowingPostAsync(Guid postId, Guid userId, CancellationToken cancellationToken = default);

    #endregion

    #region Validation and Utilities

    /// <summary>Checks if user can perform an action on a post</summary>
    Task<Result<bool>> CanUserPerformActionAsync(Guid postId, Guid userId, string action, CancellationToken cancellationToken = default);

    /// <summary>Recalculates statistics for a post</summary>
    Task<Result> RecalculateStatisticsAsync(Guid postId, CancellationToken cancellationToken = default);

    /// <summary>Recalculates trending scores for all posts</summary>
    Task<Result<int>> RecalculateAllTrendingScoresAsync(CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Service interface for system-generated announcements and automated posts
/// </summary>
public interface IPostAnnouncementService
{
    /// <summary>Creates a system announcement post</summary>
    Task<Result<Post>> CreateSystemAnnouncementAsync(
        Guid tenantId,
        Guid authorId,
        string title,
        string message,
        string priority = "normal",
        CancellationToken cancellationToken = default);

    /// <summary>Creates a milestone celebration post</summary>
    Task<Result<Post>> CreateMilestoneCelebrationAsync(
        Guid tenantId,
        Guid authorId,
        string milestoneName,
        string description,
        DateTime achievementDate,
        CancellationToken cancellationToken = default);

    /// <summary>Creates a community update post</summary>
    Task<Result<Post>> CreateCommunityUpdateAsync(
        Guid tenantId,
        Guid authorId,
        string title,
        string content,
        string targetAudience = "all",
        CancellationToken cancellationToken = default);

    /// <summary>Creates a welcome post for a new user</summary>
    Task<Result<Post>> CreateWelcomePostAsync(
        Guid tenantId,
        Guid userId,
        string userName,
        CancellationToken cancellationToken = default);
}
