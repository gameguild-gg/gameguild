namespace GameGuild.Modules.Followers.DTOs;

/// <summary> DTO for follower information </summary>
public class FollowerDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FollowedEntityId { get; set; }
    public string FollowedEntityType { get; set; } = string.Empty;
    public bool NotificationsEnabled { get; set; }
    public DateTime FollowedAt { get; set; }
}

/// <summary> DTO for follower statistics </summary>
public class FollowerStatisticsDto
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public int MutualFollowCount { get; set; }
    public DateTime? LastFollowerAddedAt { get; set; }
}

/// <summary> DTO for follower privacy settings </summary>
public class FollowerPrivacySettingsDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public bool IsFollowerListPublic { get; set; }
    public bool IsFollowingListPublic { get; set; }
    public bool AllowFollowers { get; set; }
    public bool NotifyOnNewFollower { get; set; }
    public bool ShowFollowerCount { get; set; }
    public bool ShowFollowingCount { get; set; }
}

/// <summary> DTO for blocked user information </summary>
public class BlockedUserDto
{
    public Guid Id { get; set; }
    public Guid BlockingUserId { get; set; }
    public Guid BlockedUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime BlockedAt { get; set; }
}

/// <summary> DTO for muted user information </summary>
public class MutedUserDto
{
    public Guid Id { get; set; }
    public Guid MutingUserId { get; set; }
    public Guid MutedUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime MutedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive => ExpiresAt == null || ExpiresAt > DateTime.UtcNow;
}
