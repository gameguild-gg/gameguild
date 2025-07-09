using GameGuild.Common.Domain.Entities;
using GameGuild.Modules.Tenants.Models;
using GameGuild.Modules.Users.Models;


namespace GameGuild.Modules.Followers.Models;

/// <summary>
/// Represents a follower of a followable entity.
/// </summary>
public class Follower : BaseEntity, ITenantable {
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
