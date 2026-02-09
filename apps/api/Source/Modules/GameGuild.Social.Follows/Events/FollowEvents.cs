namespace GameGuild.Social.Follows.Events;

/// <summary>Event raised when a user follows an entity</summary>
public sealed record FollowerAddedEvent(
    Guid FollowId,
    Guid FollowerId,
    Guid FollowedEntityId,
    string FollowedEntityType,
    DateTime FollowedAt,
    bool NotificationsEnabled);

/// <summary>Event raised when a user unfollows an entity</summary>
public sealed record FollowerRemovedEvent(
    Guid FollowId,
    Guid FollowerId,
    Guid FollowedEntityId,
    string FollowedEntityType,
    DateTime UnfollowedAt);

/// <summary>Event raised when a user blocks another user</summary>
public sealed record UserBlockedEvent(
    Guid BlockId,
    Guid BlockerId,
    Guid BlockedUserId,
    string? Reason,
    DateTime BlockedAt);

/// <summary>Event raised when a user unblocks another user</summary>
public sealed record UserUnblockedEvent(
    Guid BlockerId,
    Guid BlockedUserId,
    DateTime UnblockedAt);

/// <summary>Event raised when a user mutes another user</summary>
public sealed record UserMutedEvent(
    Guid MuteId,
    Guid MuterId,
    Guid MutedUserId,
    string? Reason,
    DateTime MutedAt,
    DateTime? ExpiresAt);

/// <summary>Event raised when a user unmutes another user</summary>
public sealed record UserUnmutedEvent(
    Guid MuterId,
    Guid MutedUserId,
    DateTime UnmutedAt);

/// <summary>Event raised when privacy settings are updated</summary>
public sealed record PrivacySettingsUpdatedEvent(
    Guid UserId,
    bool AllowFollowers,
    bool NotifyOnNewFollower,
    DateTime UpdatedAt);
