namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Service interface for user moderation: blocking, muting, and privacy settings.
/// </summary>
public interface IUserModerationService
{
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
