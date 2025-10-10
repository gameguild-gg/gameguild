using GameGuild.Modules.Users.Entities;

namespace GameGuild.Modules.Followers.Entities;

/// <summary> Represents a blocked user relationship </summary>
public class BlockedUser : EntityBase
{
    /// <summary> The user who is blocking </summary>
    public virtual User BlockingUser { get; set; } = null!;

    public Guid BlockingUserId { get; set; }

    /// <summary> The user being blocked </summary>
    public virtual User BlockedUserEntity { get; set; } = null!;

    public Guid BlockedUserId { get; set; }

    /// <summary> Reason for blocking (optional) </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary> Date when the block was created </summary>
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
}
