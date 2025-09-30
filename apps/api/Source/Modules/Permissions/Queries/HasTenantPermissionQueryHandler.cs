using GameGuild.CQRS;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Queries;

/// <summary>
/// Handler for checking tenant permissions
/// </summary>
public class HasTenantPermissionQueryHandler : IRequestHandler<HasTenantPermissionQuery, bool>
{
    private readonly ICachedPermissionService _permissionService;
    private readonly ILogger<HasTenantPermissionQueryHandler> _logger;

    public HasTenantPermissionQueryHandler(
        ICachedPermissionService permissionService,
        ILogger<HasTenantPermissionQueryHandler> logger)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> Handle(HasTenantPermissionQuery request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Checking tenant permission for User:{UserId} in Tenant:{TenantId}, Permission:{Permission}",
            request.UserId, request.TenantId, request.Permission);

        var hasPermission = await _permissionService.HasTenantPermissionAsync(
            request.UserId,
            request.TenantId,
            request.Permission);

        _logger.LogDebug("Permission check result: {HasPermission} for User:{UserId} in Tenant:{TenantId}, Permission:{Permission}",
            hasPermission, request.UserId, request.TenantId, request.Permission);

        return hasPermission;
    }
}