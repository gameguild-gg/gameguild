using GameGuild.Common;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Followers.Models;

/// <summary>
/// Represents a follower of a followable entity.
/// </summary>
public class Follower : Entity, ITenantable {
  /// <summary>
  /// The user who is following
  /// </summary>
  public virtual User User { get; set; } = null!;

  public Guid UserId { get; set; }

  // Optional: Tenant property for ITenantable (hide base implementation)
  public new virtual Tenant? Tenant { get; set; }

  public new bool IsGlobal {
    get => Tenant == null;
  }
}
