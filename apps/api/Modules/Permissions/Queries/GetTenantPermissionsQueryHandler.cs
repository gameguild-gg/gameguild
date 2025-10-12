using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Handler for getting tenant permissions
/// </summary>
public class GetTenantPermissionsQueryHandler : IRequestHandler<GetTenantPermissionsQuery, IEnumerable<PermissionType>>
{
    private readonly ICachedPermissionService _permissionService;
    private readonly ILogger<GetTenantPermissionsQueryHandler> _logger;

    public GetTenantPermissionsQueryHandler(
        ICachedPermissionService permissionService,
        ILogger<GetTenantPermissionsQueryHandler> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<PermissionType>> Handle(GetTenantPermissionsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogDebug("Getting tenant permissions for User:{UserId} in Tenant:{TenantId}, IncludeEffective:{IncludeEffective}",
            request.UserId, request.TenantId, request.IncludeEffectivePermissions);

        var permissions = request.IncludeEffectivePermissions
            ? await _permissionService.GetEffectiveTenantPermissionsAsync(request.UserId, request.TenantId)
            : await _permissionService.GetTenantPermissionsAsync(request.UserId, request.TenantId);

        _logger.LogDebug("Retrieved {Count} tenant permissions for User:{UserId} in Tenant:{TenantId}",
            permissions.Count(), request.UserId, request.TenantId);

        return permissions;
    }
}