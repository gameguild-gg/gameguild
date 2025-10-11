using GameGuild.Modules.Followers.Entities;

namespace GameGuild.Modules.Followers.Services;

/// <summary> Service interface for managing follower relationships </summary>
public interface IFollowerService
{
    /// <summary> Follow an entity </summary>
    /// <param name="userId">The ID of the user who wants to follow</param>
    /// <param name="entityId">The ID of the entity to follow</param>
    /// <param name="entityType">The type of entity being followed</param>
    /// <param name="notificationsEnabled">Whether to enable notifications for this relationship</param>
    /// <returns>The created Follower relationship</returns>
    Task<Follower> FollowAsync(Guid userId, Guid entityId, string entityType, bool notificationsEnabled = true);

    /// <summary> Unfollow an entity </summary>
    /// <param name="userId">The ID of the user who wants to unfollow</param>
    /// <param name="entityId">The ID of the entity to unfollow</param>
    /// <param name="entityType">The type of entity being unfollowed</param>
    /// <returns>True if successfully unfollowed</returns>
    Task<bool> UnfollowAsync(Guid userId, Guid entityId, string entityType);

    /// <summary> Check if a user is following an entity </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="entityType">The type of entity</param>
    /// <returns>True if the user is following the entity</returns>
    Task<bool> IsFollowingAsync(Guid userId, Guid entityId, string entityType);

    /// <summary> Get all followers for an entity </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="entityType">The type of entity</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <returns>List of followers</returns>
    Task<IEnumerable<Follower>> GetFollowersAsync(Guid entityId, string entityType, int skip = 0, int take = 50);

    /// <summary> Get all entities a user is following </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="entityType">Optional filter by entity type</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <returns>List of followed entities</returns>
    Task<IEnumerable<Follower>> GetFollowingAsync(Guid userId, string? entityType = null, int skip = 0, int take = 50);

    /// <summary> Get follower count for an entity </summary>
    /// <param name="entityId">The ID of the entity</param>
    /// <param name="entityType">The type of entity</param>
    /// <returns>Number of followers</returns>
    Task<int> GetFollowerCountAsync(Guid entityId, string entityType);

    /// <summary> Get following count for a user </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="entityType">Optional filter by entity type</param>
    /// <returns>Number of entities the user is following</returns>
    Task<int> GetFollowingCountAsync(Guid userId, string? entityType = null);

    /// <summary> Check if two users mutually follow each other </summary>
    /// <param name="userId1">First user ID</param>
    /// <param name="userId2">Second user ID</param>
    /// <returns>True if both users follow each other</returns>
    Task<bool> AreMutualFollowersAsync(Guid userId1, Guid userId2);

    /// <summary> Get privacy settings for a user </summary>
    /// <param name="userId">The ID of the user</param>
    /// <returns>Privacy settings or null if not found</returns>
    Task<FollowerPrivacySettings?> GetPrivacySettingsAsync(Guid userId);

    /// <summary> Update privacy settings for a user </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="settings">The privacy settings to update</param>
    /// <returns>Updated privacy settings</returns>
    Task<FollowerPrivacySettings> UpdatePrivacySettingsAsync(Guid userId, FollowerPrivacySettings settings);

    /// <summary> Block a user </summary>
    /// <param name="blockingUserId">The ID of the user who is blocking</param>
    /// <param name="blockedUserId">The ID of the user being blocked</param>
    /// <param name="reason">Optional reason for blocking</param>
    /// <returns>The created BlockedUser relationship</returns>
    Task<BlockedUser> BlockUserAsync(Guid blockingUserId, Guid blockedUserId, string? reason = null);

    /// <summary> Unblock a user </summary>
    /// <param name="blockingUserId">The ID of the user who blocked</param>
    /// <param name="blockedUserId">The ID of the user to unblock</param>
    /// <returns>True if successfully unblocked</returns>
    Task<bool> UnblockUserAsync(Guid blockingUserId, Guid blockedUserId);

    /// <summary> Check if a user has blocked another user </summary>
    /// <param name="blockingUserId">The ID of the potentially blocking user</param>
    /// <param name="blockedUserId">The ID of the potentially blocked user</param>
    /// <returns>True if blocked</returns>
    Task<bool> IsUserBlockedAsync(Guid blockingUserId, Guid blockedUserId);

    /// <summary> Mute a user </summary>
    /// <param name="mutingUserId">The ID of the user who is muting</param>
    /// <param name="mutedUserId">The ID of the user being muted</param>
    /// <param name="reason">Optional reason for muting</param>
    /// <param name="expiresAt">Optional expiration date for temporary mutes</param>
    /// <returns>The created MutedUser relationship</returns>
    Task<MutedUser> MuteUserAsync(Guid mutingUserId, Guid mutedUserId, string? reason = null, DateTime? expiresAt = null);

    /// <summary> Unmute a user </summary>
    /// <param name="mutingUserId">The ID of the user who muted</param>
    /// <param name="mutedUserId">The ID of the user to unmute</param>
    /// <returns>True if successfully unmuted</returns>
    Task<bool> UnmuteUserAsync(Guid mutingUserId, Guid mutedUserId);

    /// <summary> Check if a user has muted another user </summary>
    /// <param name="mutingUserId">The ID of the potentially muting user</param>
    /// <param name="mutedUserId">The ID of the potentially muted user</param>
    /// <returns>True if muted</returns>
    Task<bool> IsUserMutedAsync(Guid mutingUserId, Guid mutedUserId);

    /// <summary> Get blocked users list </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <returns>List of blocked users</returns>
    Task<IEnumerable<BlockedUser>> GetBlockedUsersAsync(Guid userId, int skip = 0, int take = 50);

    /// <summary> Get muted users list </summary>
    /// <param name="userId">The ID of the user</param>
    /// <param name="skip">Number of records to skip (pagination)</param>
    /// <param name="take">Number of records to take (pagination)</param>
    /// <returns>List of muted users</returns>
    Task<IEnumerable<MutedUser>> GetMutedUsersAsync(Guid userId, int skip = 0, int take = 50);
}
