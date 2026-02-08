namespace GameGuild.Identity.Authorization;

/// <summary>
///     Adapter that bridges the authorization permission service interface
///     to the permission query service.
/// </summary>
public sealed class AuthorizationPermissionServiceAdapter : IAuthorizationPermissionService
{
    private readonly IPermissionQueryService _queryService;

    /// <summary>
    ///     Initializes a new instance of <see cref="AuthorizationPermissionServiceAdapter"/>.
    /// </summary>
    public AuthorizationPermissionServiceAdapter(IPermissionQueryService queryService)
    {
        _queryService = queryService;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return await _queryService.HasTenantPermissionAsync(
            userId,
            tenantId,
            permission,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> HasAllPermissionsAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var permissionList = permissions.ToList();
        if (permissionList.Count == 0)
        {
            return PermissionCheckResult.AllPresent([]);
        }

        // Get all user permissions once (single DB call)
        var userPermissions = await _queryService.GetEffectivePermissionsAsync(
            userId,
            tenantId,
            cancellationToken).ConfigureAwait(false);

        var userPermissionSet = new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);

        var present = permissionList.Where(p => userPermissionSet.Contains(p)).ToList();
        var missing = permissionList.Where(p => !userPermissionSet.Contains(p)).ToList();

        if (missing.Count == 0)
        {
            return PermissionCheckResult.AllPresent(present);
        }

        return present.Count > 0
            ? PermissionCheckResult.Partial(present, missing)
            : PermissionCheckResult.NonePresent(missing);
    }

    /// <inheritdoc />
    public async Task<PermissionCheckResult> HasAnyPermissionAsync(
        Guid userId,
        Guid tenantId,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default)
    {
        var permissionList = permissions.ToList();
        if (permissionList.Count == 0)
        {
            return PermissionCheckResult.NonePresent([]);
        }

        // Get all user permissions once (single DB call)
        var userPermissions = await _queryService.GetEffectivePermissionsAsync(
            userId,
            tenantId,
            cancellationToken).ConfigureAwait(false);

        var userPermissionSet = new HashSet<string>(userPermissions, StringComparer.OrdinalIgnoreCase);

        var present = permissionList.Where(p => userPermissionSet.Contains(p)).ToList();
        var missing = permissionList.Where(p => !userPermissionSet.Contains(p)).ToList();

        if (present.Count == permissionList.Count)
        {
            return PermissionCheckResult.AllPresent(present);
        }

        return present.Count > 0
            ? PermissionCheckResult.Partial(present, missing)
            : PermissionCheckResult.NonePresent(missing);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var permissions = await _queryService.GetEffectivePermissionsAsync(
            userId,
            tenantId,
            cancellationToken).ConfigureAwait(false);

        return permissions;
    }
}
