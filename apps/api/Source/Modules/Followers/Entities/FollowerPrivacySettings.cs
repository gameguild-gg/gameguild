using GameGuild.Modules.Users;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Followers.Entities;

/// <summary> Represents privacy settings for follower relationships </summary>
public class FollowerPrivacySettings : EntityBase
{
    /// <summary> The user these settings belong to </summary>
    public virtual User User { get; set; } = null!;

    public Guid UserId { get; set; }

    /// <summary> Whether the user's follower list is public </summary>
    public bool IsFollowerListPublic { get; set; } = true;

    /// <summary> Whether the user's following list is public </summary>
    public bool IsFollowingListPublic { get; set; } = true;

    /// <summary> Whether anyone can follow this user </summary>
    public bool AllowFollowers { get; set; } = true;

    /// <summary> Whether to send notifications when someone follows </summary>
    public bool NotifyOnNewFollower { get; set; } = true;

    /// <summary> Whether to show follower count publicly </summary>
    public bool ShowFollowerCount { get; set; } = true;

    /// <summary> Whether to show following count publicly </summary>
    public bool ShowFollowingCount { get; set; } = true;
}
