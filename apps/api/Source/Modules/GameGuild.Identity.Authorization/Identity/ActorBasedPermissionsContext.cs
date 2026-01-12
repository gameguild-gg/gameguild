using GameGuild.Identity.Context.Actors;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Adapter that implements <see cref="IPermissionsContext"/> on top of <see cref="ActorContext"/>.
/// </summary>
/// <remarks>
///     <para>
///         This adapter allows gradual migration from the service-based PermissionsContext
///         to the new ActorContext model. The main difference is that ActorContext contains
///         pre-evaluated permissions, while the old PermissionsContext fetches them on demand.
///     </para>
///     <para>
///         For resource-level permissions that require database lookups, this adapter
///         delegates to the underlying <see cref="IPermissionService"/>.
///     </para>
/// </remarks>
[Obsolete("Use IActorContextAccessor for new code. This adapter is provided for backward compatibility.")]
public sealed class ActorBasedPermissionsContext : IPermissionsContext
{
    private readonly IActorContextAccessor _actorContextAccessor;
    private readonly IPermissionService _permissionService;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ActorBasedPermissionsContext"/> class.
    /// </summary>
    public ActorBasedPermissionsContext(
        IActorContextAccessor actorContextAccessor,
        IPermissionService permissionService)
    {
        _actorContextAccessor = actorContextAccessor ?? throw new ArgumentNullException(nameof(actorContextAccessor));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
    }

    private ActorContext Context => _actorContextAccessor.ActorContext;

    /// <inheritdoc />
    public Guid? UserId => Context.SubjectIdAsGuid;

    /// <inheritdoc />
    public Guid? TenantId => Context.TenantId;

    /// <inheritdoc />
    public bool IsAuthenticated => Context.IsAuthenticated;

    /// <inheritdoc />
    public bool IsSystemAdmin => Context.IsSystemAdmin;

    /// <inheritdoc />
    public bool IsTenantAdmin => Context.IsTenantAdmin;

    /// <inheritdoc />
    public Task<bool> HasTenantPermissionAsync(string permission, Guid? tenantId = null)
    {
        if (!Context.IsAuthenticated) 
            return Task.FromResult(false);

        // System admins have all permissions
        if (IsSystemAdmin) 
            return Task.FromResult(true);

        // Check against pre-evaluated permissions in ActorContext
        // Note: This assumes permissions were loaded for the current tenant
        var effectiveTenantId = tenantId ?? TenantId;
        
        // If checking a different tenant than current, we can't use cached permissions
        if (effectiveTenantId.HasValue && effectiveTenantId != TenantId)
        {
            // Delegate to permission service for cross-tenant checks
            if (UserId.HasValue)
            {
                return _permissionService.HasTenantPermissionAsync(
                    UserId, 
                    effectiveTenantId, 
                    permission);
            }
            return Task.FromResult(false);
        }

        // Use pre-evaluated permissions from ActorContext
        return Task.FromResult(Context.HasPermission(permission));
    }

    /// <inheritdoc />
    public async Task<bool> HasResourcePermissionAsync(string resourceType, Guid resourceId, string permission)
    {
        if (!UserId.HasValue) return false;
        if (!TenantId.HasValue) return false;

        // System admins have all permissions
        if (IsSystemAdmin) return true;

        // For resource-level permissions, we still need to check the database
        // as these are not pre-loaded into ActorContext
        var resourcePermission = $"{resourceType}.{resourceId}.{permission}";

        return await _permissionService.HasTenantPermissionAsync(
            UserId.Value, 
            TenantId.Value, 
            resourcePermission);
    }

    /// <inheritdoc />
    public Task<List<string>> GetEffectivePermissionsAsync()
    {
        // Return pre-evaluated permissions from ActorContext
        return Task.FromResult(Context.Permissions.ToList());
    }

    /// <inheritdoc />
    public bool IsOwner(Guid? resourceOwnerId)
    {
        if (!UserId.HasValue || !resourceOwnerId.HasValue) return false;

        return UserId.Value == resourceOwnerId.Value;
    }
}
