using GameGuild.Modules.Users;
using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Followers.Entities;

/// <summary> Represents a muted user relationship </summary>
public class MutedUser : EntityBase
{
    /// <summary> The user who is muting </summary>
    public virtual User MutingUser { get; set; } = null!;

    public Guid MutingUserId { get; set; }

    /// <summary> The user being muted </summary>
    public virtual User MutedUserEntity { get; set; } = null!;

    public Guid MutedUserId { get; set; }

    /// <summary> Reason for muting (optional) </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary> Date when the mute was created </summary>
    public DateTime MutedAt { get; set; } = DateTime.UtcNow;

    /// <summary> Optional expiration date for temporary mutes </summary>
    public DateTime? ExpiresAt { get; set; }
}
