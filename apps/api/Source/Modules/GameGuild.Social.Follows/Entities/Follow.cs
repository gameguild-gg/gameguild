using System.ComponentModel.DataAnnotations;

namespace GameGuild.Social.Follows;

/// <summary>
/// Represents a polymorphic follow relationship - users can follow other users, courses, projects, etc.
/// </summary>
public class Follow : EntityBase
{
    /// <summary>The ID of the user who is following</summary>
    public Guid FollowerId { get; private set; }

    /// <summary>The ID of the entity being followed</summary>
    public Guid FollowedEntityId { get; private set; }

    /// <summary>The type of entity being followed (User, Course, Project, etc.)</summary>
    [MaxLength(100)]
    public string FollowedEntityType { get; private set; } = string.Empty;

    /// <summary>Whether notifications are enabled for this follow relationship</summary>
    public bool NotificationsEnabled { get; private set; }

    /// <summary>When the follow relationship was created</summary>
    public DateTime FollowedAt { get; private set; }

    private Follow() { } // EF Core

    public static Follow Create(Guid followerId, Guid followedEntityId, string followedEntityType, bool notificationsEnabled = true)
    {
        return new Follow
        {
            Id = Guid.NewGuid(),
            FollowerId = followerId,
            FollowedEntityId = followedEntityId,
            FollowedEntityType = followedEntityType,
            NotificationsEnabled = notificationsEnabled,
            FollowedAt = DateTime.UtcNow
        };
    }

    public void EnableNotifications()
    {
        NotificationsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DisableNotifications()
    {
        NotificationsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateNotificationSettings(bool enabled)
    {
        NotificationsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a blocked user relationship - blocking removes all follow relationships and prevents future follows
/// </summary>
public class Block : EntityBase
{
    /// <summary>The ID of the user who is blocking</summary>
    public Guid BlockerId { get; private set; }

    /// <summary>The ID of the user being blocked</summary>
    public Guid BlockedId { get; private set; }

    /// <summary>Optional reason for the block</summary>
    [MaxLength(500)]
    public string? Reason { get; private set; }

    /// <summary>When the block was created</summary>
    public DateTime BlockedAt { get; private set; }

    private Block() { } // EF Core

    public static Block Create(Guid blockerId, Guid blockedId, string? reason = null)
    {
        return new Block
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedId = blockedId,
            Reason = reason,
            BlockedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Represents a muted user relationship - muting hides content from a user without blocking them
/// </summary>
public class Mute : EntityBase
{
    /// <summary>The ID of the user who is muting</summary>
    public Guid MuterId { get; private set; }

    /// <summary>The ID of the user being muted</summary>
    public Guid MutedId { get; private set; }

    /// <summary>Optional reason for the mute</summary>
    [MaxLength(500)]
    public string? Reason { get; private set; }

    /// <summary>When the mute was created</summary>
    public DateTime MutedAt { get; private set; }

    /// <summary>Optional expiration date for temporary mutes</summary>
    public DateTime? ExpiresAt { get; private set; }

    private Mute() { } // EF Core

    public static Mute Create(Guid muterId, Guid mutedId, string? reason = null, DateTime? expiresAt = null)
    {
        return new Mute
        {
            Id = Guid.NewGuid(),
            MuterId = muterId,
            MutedId = mutedId,
            Reason = reason,
            MutedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>Check if this mute has expired</summary>
    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;

    /// <summary>Extend the mute expiration</summary>
    public void ExtendExpiration(DateTime? newExpiresAt)
    {
        ExpiresAt = newExpiresAt;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Privacy settings for a user's follow relationships
/// </summary>
public class FollowPrivacySettings : EntityBase
{
    /// <summary>The user these settings belong to</summary>
    public Guid UserId { get; private set; }

    /// <summary>Whether the user's follower list is publicly visible</summary>
    public bool IsFollowerListPublic { get; private set; } = true;

    /// <summary>Whether the user's following list is publicly visible</summary>
    public bool IsFollowingListPublic { get; private set; } = true;

    /// <summary>Whether anyone can follow this user</summary>
    public bool AllowFollowers { get; private set; } = true;

    /// <summary>Whether to send notifications when someone follows</summary>
    public bool NotifyOnNewFollower { get; private set; } = true;

    /// <summary>Whether to show the follower count publicly</summary>
    public bool ShowFollowerCount { get; private set; } = true;

    /// <summary>Whether to show the following count publicly</summary>
    public bool ShowFollowingCount { get; private set; } = true;

    private FollowPrivacySettings() { } // EF Core

    public static FollowPrivacySettings CreateDefault(Guid userId)
    {
        return new FollowPrivacySettings
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IsFollowerListPublic = true,
            IsFollowingListPublic = true,
            AllowFollowers = true,
            NotifyOnNewFollower = true,
            ShowFollowerCount = true,
            ShowFollowingCount = true
        };
    }

    public void Update(
        bool isFollowerListPublic,
        bool isFollowingListPublic,
        bool allowFollowers,
        bool notifyOnNewFollower,
        bool showFollowerCount,
        bool showFollowingCount)
    {
        IsFollowerListPublic = isFollowerListPublic;
        IsFollowingListPublic = isFollowingListPublic;
        AllowFollowers = allowFollowers;
        NotifyOnNewFollower = notifyOnNewFollower;
        ShowFollowerCount = showFollowerCount;
        ShowFollowingCount = showFollowingCount;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Interface for entities that can be followed
/// </summary>
public interface IFollowable
{
    Guid Id { get; }
    string GetFollowableType();
}
