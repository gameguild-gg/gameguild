using System.Linq.Expressions;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Specifications;

namespace GameGuild.Modules.Permissions.Specifications;

/// <summary>
/// Specification for checking if a user has a specific tenant permission
/// </summary>
public class UserHasPermissionSpecification : Specification<TenantPermission>
{
    private readonly Guid _userId;
    private readonly Guid _tenantId;
    private readonly PermissionType _permission;

    public UserHasPermissionSpecification(Guid userId, Guid tenantId, PermissionType permission)
    {
        _userId = userId;
        _tenantId = tenantId;
        _permission = permission;
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.UserId == _userId
            && tp.TenantId == _tenantId
            && tp.DeletedAt == null
            && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow)
            && tp.HasPermission(_permission);
    }
}

/// <summary>
/// Specification for active (non-expired, non-deleted) permissions
/// </summary>
public class ActivePermissionSpecification : Specification<TenantPermission>
{
    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.DeletedAt == null
            && (tp.ExpiresAt == null || tp.ExpiresAt > DateTime.UtcNow);
    }
}

/// <summary>
/// Specification for default permissions (no specific user assigned)
/// </summary>
public class DefaultPermissionSpecification : Specification<TenantPermission>
{
    private readonly Guid _tenantId;

    public DefaultPermissionSpecification(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.UserId == null
            && tp.TenantId == _tenantId
            && tp.DeletedAt == null;
    }
}

/// <summary>
/// Specification for global default permissions (no tenant and no user)
/// </summary>
public class GlobalDefaultPermissionSpecification : Specification<TenantPermission>
{
    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.UserId == null
            && tp.TenantId == null
            && tp.DeletedAt == null;
    }
}

/// <summary>
/// Specification for permissions expiring within a specific timeframe
/// </summary>
public class ExpiringPermissionSpecification : Specification<TenantPermission>
{
    private readonly DateTime _expirationThreshold;

    public ExpiringPermissionSpecification(TimeSpan withinTimespan)
    {
        _expirationThreshold = DateTime.UtcNow.Add(withinTimespan);
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.ExpiresAt != null
            && tp.ExpiresAt <= _expirationThreshold
            && tp.DeletedAt == null;
    }
}

/// <summary>
/// Specification for permissions of a specific user across all tenants
/// </summary>
public class UserPermissionsSpecification : Specification<TenantPermission>
{
    private readonly Guid _userId;

    public UserPermissionsSpecification(Guid userId)
    {
        _userId = userId;
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.UserId == _userId
            && tp.DeletedAt == null;
    }
}

/// <summary>
/// Specification for permissions within a specific tenant
/// </summary>
public class TenantPermissionsSpecification : Specification<TenantPermission>
{
    private readonly Guid _tenantId;

    public TenantPermissionsSpecification(Guid tenantId)
    {
        _tenantId = tenantId;
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.TenantId == _tenantId
            && tp.DeletedAt == null;
    }
}

/// <summary>
/// Specification for permissions that contain any of the specified permission types
/// </summary>
public class HasAnyPermissionSpecification : Specification<TenantPermission>
{
    private readonly PermissionType[] _permissions;

    public HasAnyPermissionSpecification(params PermissionType[] permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.DeletedAt == null
            && _permissions.Any(p => tp.HasPermission(p));
    }
}

/// <summary>
/// Specification for permissions that contain all of the specified permission types
/// </summary>
public class HasAllPermissionsSpecification : Specification<TenantPermission>
{
    private readonly PermissionType[] _permissions;

    public HasAllPermissionsSpecification(params PermissionType[] permissions)
    {
        _permissions = permissions ?? throw new ArgumentNullException(nameof(permissions));
    }

    public override Expression<Func<TenantPermission, bool>> ToExpression()
    {
        return tp => tp.DeletedAt == null
            && _permissions.All(p => tp.HasPermission(p));
    }
}