using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.User.Models;

namespace GameGuild.Common.Entities;

/// <summary>
/// Represents a follower of a followable entity.
/// </summary>
public class Follower : BaseEntity, ITenantable
{
    private User _user = null!;

    private Guid _userId;

    private Tenant? _tenant;

    /// <summary>
    /// The user who is following
    /// </summary>
    public virtual User User
    {
        get => _user;
        set => _user = value;
    }

    public Guid UserId
    {
        get => _userId;
        set => _userId = value;
    }

    // Optional: Tenant property for ITenantable (hide base implementation)
    public new virtual Tenant? Tenant
    {
        get => _tenant;
        set => _tenant = value;
    }

    public new bool IsGlobal
    {
        get => Tenant == null;
    }
}
