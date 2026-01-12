using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Implementation of <see cref="IPermissionGrantService"/> that delegates to <see cref="IPermissionService"/>.
/// </summary>
/// <remarks>
///     <para>
///         This is an adapter that provides SRP-compliant interfaces while maintaining backward compatibility
///         with the existing <c>PermissionService</c>. New code should use the focused interfaces
///         (<c>IPermissionGrantService</c>, <c>IPermissionQueryService</c>, <c>IPermissionBulkService</c>).
///     </para>
/// </remarks>
public sealed class PermissionGrantService : IPermissionGrantService
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionGrantService> _logger;

    public PermissionGrantService(
        IPermissionService permissionService,
        ILogger<PermissionGrantService> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<TenantPermission> GrantTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        DateTime? expiresAt = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.GrantTenantPermissionAsync(
            userId, tenantId, permissions, grantedBy, expiresAt, reason, cancellationToken);
    }

    public Task<bool> RevokeTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string[] permissions,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.RevokeTenantPermissionAsync(
            userId, tenantId, permissions, cancellationToken);
    }

    public Task SetGlobalDefaultPermissionsAsync(
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.SetGlobalDefaultPermissionsAsync(
            permissions, setBy, cancellationToken);
    }

    public Task SetTenantDefaultPermissionsAsync(
        Guid tenantId,
        string[] permissions,
        Guid? setBy = null,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.SetTenantDefaultPermissionsAsync(
            tenantId, permissions, setBy, cancellationToken);
    }
}

/// <summary>
///     Implementation of <see cref="IPermissionQueryService"/> that delegates to <see cref="IPermissionService"/>.
/// </summary>
public sealed class PermissionQueryService : IPermissionQueryService
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionQueryService> _logger;

    public PermissionQueryService(
        IPermissionService permissionService,
        ILogger<PermissionQueryService> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<bool> HasTenantPermissionAsync(
        Guid? userId,
        Guid? tenantId,
        string permission,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.HasTenantPermissionAsync(
            userId, tenantId, permission, cancellationToken);
    }

    public Task<List<string>> GetTenantPermissionsAsync(
        Guid? userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.GetTenantPermissionsAsync(
            userId, tenantId, cancellationToken);
    }

    public Task<List<string>> GetEffectivePermissionsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.GetEffectivePermissionsAsync(
            userId, tenantId, cancellationToken);
    }

    public Task<List<string>> GetGlobalDefaultPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        return _permissionService.GetGlobalDefaultPermissionsAsync(cancellationToken);
    }

    public Task<List<string>> GetTenantDefaultPermissionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.GetTenantDefaultPermissionsAsync(
            tenantId, cancellationToken);
    }

    public Task<bool> IsUserInTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.IsUserInTenantAsync(
            userId, tenantId, cancellationToken);
    }
}

/// <summary>
///     Implementation of <see cref="IPermissionBulkService"/> that delegates to <see cref="IPermissionService"/>.
/// </summary>
public sealed class PermissionBulkService : IPermissionBulkService
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionBulkService> _logger;

    public PermissionBulkService(
        IPermissionService permissionService,
        ILogger<PermissionBulkService> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<List<TenantPermission>> BulkGrantTenantPermissionAsync(
        Guid[] userIds,
        Guid tenantId,
        string[] permissions,
        Guid? grantedBy = null,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.BulkGrantTenantPermissionAsync(
            userIds, tenantId, permissions, grantedBy, cancellationToken);
    }

    public Task<TenantPermission> JoinTenantAsync(
        Guid userId,
        Guid tenantId,
        Guid? invitedBy = null,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.JoinTenantAsync(
            userId, tenantId, invitedBy, cancellationToken);
    }

    public Task<bool> LeaveTenantAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return _permissionService.LeaveTenantAsync(
            userId, tenantId, cancellationToken);
    }
}
