using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users.Entities;


namespace GameGuild.Modules.Followers.Entities;

/// <summary> Represents a follower of a followable entity. </summary>
public class Follower : EntityBase, ITenantable
{
    /// <summary> The user who is following </summary>
    public virtual User User { get; set; } = null!;

    public Guid UserId { get; set; }

    /// <summary> The ID of the entity being followed </summary>
    public Guid FollowedEntityId { get; set; }

    /// <summary> The type of the entity being followed (for polymorphic relationships) </summary>
    [MaxLength(255)]
    public string FollowedEntityType { get; set; } = string.Empty;

    /// <summary> Indicates whether notifications are enabled for this follower relationship </summary>
    public bool NotificationsEnabled { get; set; } = true;

    /// <summary> Date when the user started following </summary>
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;

    // Optional: Tenant property for ITenantable (hide base implementation)
    public new virtual Tenant? Tenant { get; set; }

    public new bool IsGlobal { get => Tenant == null; }
}

