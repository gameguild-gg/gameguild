namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Service interface for core follow/unfollow operations, queries, and batch lookups.
/// </summary>
public interface IFollowOperationService
{
    /// <summary>Follow an entity (user, course, project, etc.)</summary>
    Task<Result<Follow>> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true, CancellationToken ct = default);

    /// <summary>Unfollow an entity</summary>
    Task<Result> UnfollowAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Check if a user is following an entity</summary>
    Task<Result<bool>> IsFollowingAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Update notification settings for a follow relationship</summary>
    Task<Result<Follow>> UpdateNotificationSettingsAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled, CancellationToken ct = default);

    /// <summary>Get all followers for an entity</summary>
    Task<Result<List<Follow>>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get all entities a user is following</summary>
    Task<Result<List<Follow>>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Get follower count for an entity</summary>
    Task<Result<int>> GetFollowerCountAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Get following count for a user</summary>
    Task<Result<int>> GetFollowingCountAsync(Guid userId, string? entityType = null, CancellationToken ct = default);

    /// <summary>Check if two users mutually follow each other</summary>
    Task<Result<bool>> AreMutualFollowersAsync(Guid userId1, Guid userId2, CancellationToken ct = default);

    /// <summary>Get follow relationship by ID</summary>
    Task<Result<Follow>> GetFollowByIdAsync(Guid followId, CancellationToken ct = default);

    /// <summary>Get users who follow an entity and have notifications enabled</summary>
    Task<Result<List<Follow>>> GetFollowersWithNotificationsAsync(Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Get follow status for multiple entities at once (for DataLoader)</summary>
    Task<Result<Dictionary<Guid, bool>>> GetFollowStatusBatchAsync(Guid userId, IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default);

    /// <summary>Get follower counts for multiple entities at once (for DataLoader)</summary>
    Task<Result<Dictionary<Guid, int>>> GetFollowerCountsBatchAsync(IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default);
}
