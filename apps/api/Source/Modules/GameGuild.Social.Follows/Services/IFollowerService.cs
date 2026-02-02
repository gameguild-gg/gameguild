using GameGuild.Models;

namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Service interface for managing follower relationships, blocking, muting, and privacy settings
/// </summary>
public interface IFollowerService
{
    #region Follow Operations

    /// <summary>Follow an entity (user, course, project, etc.)</summary>
    Task<Result<Follow>> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true, CancellationToken ct = default);

    /// <summary>Unfollow an entity</summary>
    Task<Result> UnfollowAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Check if a user is following an entity</summary>
    Task<Result<bool>> IsFollowingAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default);

    /// <summary>Update notification settings for a follow relationship</summary>
    Task<Result<Follow>> UpdateNotificationSettingsAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled, CancellationToken ct = default);

    #endregion

    #region Query Operations

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

    #endregion

    #region Batch Operations

    /// <summary>Get follow status for multiple entities at once (for DataLoader)</summary>
    Task<Result<Dictionary<Guid, bool>>> GetFollowStatusBatchAsync(Guid userId, IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default);

    /// <summary>Get follower counts for multiple entities at once (for DataLoader)</summary>
    Task<Result<Dictionary<Guid, int>>> GetFollowerCountsBatchAsync(IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default);

    #endregion

    #region Privacy Settings

    /// <summary>Get privacy settings for a user</summary>
    Task<Result<FollowPrivacySettings>> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Update privacy settings for a user</summary>
    Task<Result<FollowPrivacySettings>> UpdatePrivacySettingsAsync(
        Guid userId,
        bool isFollowerListPublic,
        bool isFollowingListPublic,
        bool allowFollowers,
        bool notifyOnNewFollower,
        bool showFollowerCount,
        bool showFollowingCount,
        CancellationToken ct = default);

    #endregion

    #region Block Operations

    /// <summary>Block a user - removes all follow relationships and prevents future follows</summary>
    Task<Result<Block>> BlockUserAsync(Guid blockingUserId, Guid blockedUserId, string? reason = null, CancellationToken ct = default);

    /// <summary>Unblock a user</summary>
    Task<Result> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default);

    /// <summary>Check if a user has blocked another user</summary>
    Task<Result<bool>> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default);

    /// <summary>Check if either user has blocked the other (bidirectional check)</summary>
    Task<Result<bool>> AreUsersBlockedAsync(Guid userId1, Guid userId2, CancellationToken ct = default);

    /// <summary>Get list of users blocked by a user</summary>
    Task<Result<List<Block>>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default);

    #endregion

    #region Mute Operations

    /// <summary>Mute a user - hides their content without blocking</summary>
    Task<Result<Mute>> MuteUserAsync(Guid mutingUserId, Guid mutedUserId, string? reason = null, DateTime? expiresAt = null, CancellationToken ct = default);

    /// <summary>Unmute a user</summary>
    Task<Result> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default);

    /// <summary>Check if a user has muted another user (respects expiration)</summary>
    Task<Result<bool>> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default);

    /// <summary>Get list of users muted by a user</summary>
    Task<Result<List<Mute>>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default);

    /// <summary>Cleanup expired mutes</summary>
    Task<Result<int>> CleanupExpiredMutesAsync(CancellationToken ct = default);

    #endregion
}
