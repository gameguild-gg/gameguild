using GameGuild.CQRS;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Commands;

namespace GameGuild.Modules.Permissions.Handlers;

/// <summary>
/// Handler for granting tenant permissions to users
/// </summary>
public class GrantTenantPermissionHandler : IRequestHandler<GrantTenantPermissionCommand, TenantPermission>
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionAuditService _auditService;
    private readonly ICachedPermissionService _permissionService;
    private readonly ILogger<GrantTenantPermissionHandler> _logger;

    public GrantTenantPermissionHandler(
        ApplicationDbContext context,
        IPermissionAuditService auditService,
        ICachedPermissionService permissionService,
        ILogger<GrantTenantPermissionHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<TenantPermission> Handle(GrantTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        _logger.LogInformation("Granting permissions {Permissions} to User:{UserId} in Tenant:{TenantId}",
            string.Join(",", request.Permissions), request.UserId, request.TenantId);

        // Check if permission record already exists
        var existingPermission = await _context.TenantPermissions
            .FirstOrDefaultAsync(tp => tp.UserId == request.UserId && tp.TenantId == request.TenantId,
                cancellationToken);

        TenantPermission permission;

        if (existingPermission != null)
        {
            // Update existing permission
            foreach (var permissionType in request.Permissions)
            {
                existingPermission.AddPermission(permissionType);
            }

            if (request.ExpiresAt.HasValue)
            {
                existingPermission.ExpiresAt = request.ExpiresAt;
            }

            permission = existingPermission;
        }
        else
        {
            // Create new permission record
            permission = new TenantPermission(request.UserId, request.TenantId)
            {
                ExpiresAt = request.ExpiresAt
            };

            foreach (var permissionType in request.Permissions)
            {
                permission.AddPermission(permissionType);
            }

            _context.TenantPermissions.Add(permission);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cache
        await _permissionService.InvalidateUserPermissionCacheAsync(request.UserId, request.TenantId);

        // Log audit trail
        await _auditService.LogPermissionGrantedAsync(
            userId: request.UserId,
            tenantId: request.TenantId,
            resourceId: null,
            operation: "Grant",
            permissions: request.Permissions,
            reason: request.Reason ?? "Permissions granted via command");

        _logger.LogInformation("Successfully granted permissions to User:{UserId} in Tenant:{TenantId}",
            request.UserId, request.TenantId);

        return permission;
    }
}