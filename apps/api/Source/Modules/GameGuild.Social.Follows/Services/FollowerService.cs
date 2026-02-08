namespace GameGuild.Social.Follows.Services;

/// <summary>
/// Thin facade that delegates to <see cref="IFollowOperationService"/> and
/// <see cref="IUserModerationService"/>. Keeps the <see cref="IFollowerService"/>
/// contract intact for backward compatibility.
/// </summary>
public class FollowerService(
    IFollowOperationService followOps,
    IUserModerationService moderation) : IFollowerService
{
    #region Follow Operations

    public Task<Result<Follow>> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true, CancellationToken ct = default)
        => followOps.FollowAsync(userId, entityId, entityType, notificationsEnabled, ct);

    public Task<Result> UnfollowAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
        => followOps.UnfollowAsync(userId, entityId, entityType, ct);

    public Task<Result<bool>> IsFollowingAsync(Guid userId, Guid entityId, string entityType, CancellationToken ct = default)
        => followOps.IsFollowingAsync(userId, entityId, entityType, ct);

    public Task<Result<Follow>> UpdateNotificationSettingsAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled, CancellationToken ct = default)
        => followOps.UpdateNotificationSettingsAsync(userId, entityId, entityType, notificationsEnabled, ct);

    #endregion

    #region Query Operations

    public Task<Result<List<Follow>>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50, CancellationToken ct = default)
        => followOps.GetFollowersAsync(entityId, entityType, skip, take, ct);

    public Task<Result<List<Follow>>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50, CancellationToken ct = default)
        => followOps.GetFollowingAsync(userId, entityType, skip, take, ct);

    public Task<Result<int>> GetFollowerCountAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => followOps.GetFollowerCountAsync(entityId, entityType, ct);

    public Task<Result<int>> GetFollowingCountAsync(Guid userId, string? entityType = null, CancellationToken ct = default)
        => followOps.GetFollowingCountAsync(userId, entityType, ct);

    public Task<Result<bool>> AreMutualFollowersAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => followOps.AreMutualFollowersAsync(userId1, userId2, ct);

    public Task<Result<Follow>> GetFollowByIdAsync(Guid followId, CancellationToken ct = default)
        => followOps.GetFollowByIdAsync(followId, ct);

    public Task<Result<List<Follow>>> GetFollowersWithNotificationsAsync(Guid entityId, string entityType, CancellationToken ct = default)
        => followOps.GetFollowersWithNotificationsAsync(entityId, entityType, ct);

    #endregion

    #region Batch Operations

    public Task<Result<Dictionary<Guid, bool>>> GetFollowStatusBatchAsync(Guid userId, IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
        => followOps.GetFollowStatusBatchAsync(userId, entityIds, entityType, ct);

    public Task<Result<Dictionary<Guid, int>>> GetFollowerCountsBatchAsync(IEnumerable<Guid> entityIds, string entityType, CancellationToken ct = default)
        => followOps.GetFollowerCountsBatchAsync(entityIds, entityType, ct);

    #endregion

    #region Privacy Settings

    public Task<Result<FollowPrivacySettings>> GetPrivacySettingsAsync(Guid userId, CancellationToken ct = default)
        => moderation.GetPrivacySettingsAsync(userId, ct);

    public Task<Result<FollowPrivacySettings>> UpdatePrivacySettingsAsync(
        Guid userId, bool isFollowerListPublic, bool isFollowingListPublic,
        bool allowFollowers, bool notifyOnNewFollower, bool showFollowerCount,
        bool showFollowingCount, CancellationToken ct = default)
        => moderation.UpdatePrivacySettingsAsync(userId, isFollowerListPublic, isFollowingListPublic,
            allowFollowers, notifyOnNewFollower, showFollowerCount, showFollowingCount, ct);

    #endregion

    #region Block Operations

    public Task<Result<Block>> BlockUserAsync(Guid blockingUserId, Guid blockedUserId, string? reason = null, CancellationToken ct = default)
        => moderation.BlockUserAsync(blockingUserId, blockedUserId, reason, ct);

    public Task<Result> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
        => moderation.UnblockUserAsync(blockingUserId, blockedUserId, ct);

    public Task<Result<bool>> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId, CancellationToken ct = default)
        => moderation.IsUserBlockedAsync(blockingUserId, blockedUserId, ct);

    public Task<Result<bool>> AreUsersBlockedAsync(Guid userId1, Guid userId2, CancellationToken ct = default)
        => moderation.AreUsersBlockedAsync(userId1, userId2, ct);

    public Task<Result<List<Block>>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
        => moderation.GetBlockedUsersAsync(userId, skip, take, ct);

    #endregion

    #region Mute Operations

    public Task<Result<Mute>> MuteUserAsync(Guid mutingUserId, Guid mutedUserId, string? reason = null, DateTime? expiresAt = null, CancellationToken ct = default)
        => moderation.MuteUserAsync(mutingUserId, mutedUserId, reason, expiresAt, ct);

    public Task<Result> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
        => moderation.UnmuteUserAsync(mutingUserId, mutedUserId, ct);

    public Task<Result<bool>> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId, CancellationToken ct = default)
        => moderation.IsUserMutedAsync(mutingUserId, mutedUserId, ct);

    public Task<Result<List<Mute>>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50, CancellationToken ct = default)
        => moderation.GetMutedUsersAsync(userId, skip, take, ct);

    public Task<Result<int>> CleanupExpiredMutesAsync(CancellationToken ct = default)
        => moderation.CleanupExpiredMutesAsync(ct);

    #endregion
}

/// <summary>
/// Error codes for follower operations
/// </summary>
public static class FollowerErrors
{
    public static readonly Error FollowNotFound = Error.NotFound("Follower.NotFound", "Follow relationship not found");
    public static readonly Error BlockNotFound = Error.NotFound("Block.NotFound", "Block relationship not found");
    public static readonly Error MuteNotFound = Error.NotFound("Mute.NotFound", "Mute relationship not found");
    public static readonly Error CannotFollowBlockedUser = Error.Failure("Follower.Blocked", "Cannot follow a user who has blocked you or you have blocked");
    public static readonly Error UserDoesNotAllowFollowers = Error.Failure("Follower.NotAllowed", "This user does not allow followers");
    public static readonly Error CannotBlockYourself = Error.Failure("Block.Self", "Cannot block yourself");
    public static readonly Error CannotMuteYourself = Error.Failure("Mute.Self", "Cannot mute yourself");
}

/// <summary>
/// Constants for followable entity types
/// </summary>
public static class FollowableEntityTypes
{
    public const string User = "User";
    public const string Course = "Course";
    public const string Project = "Project";
    public const string Program = "Program";
    public const string Tag = "Tag";
    public const string Team = "Team";
}
