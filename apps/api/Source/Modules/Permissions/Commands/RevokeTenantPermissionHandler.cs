using GameGuild.Core.Logging;
using GameGuild.CQRS;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Commands;

/// <summary>
/// Handler for revoking tenant permissions from users
/// </summary>
public class RevokeTenantPermissionHandler : IRequestHandler<RevokeTenantPermissionCommand>
{
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RevokeTenantPermissionHandler> _logger;
    private readonly IPermissionAuditService _auditService;

    public RevokeTenantPermissionHandler(
        IPermissionService permissionService,
        ILogger<RevokeTenantPermissionHandler> logger,
        IPermissionAuditService auditService)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }

    public async Task<Unit> Handle(RevokeTenantPermissionCommand request, CancellationToken cancellationToken)
    {
        using var userContext = LoggingExtensions.WithUserContext(request.UserId, request.TenantId);

        _logger.LogInformation("Revoking tenant permissions for User:{UserId} in Tenant:{TenantId}, Permissions:{Permissions}",
            request.UserId, request.TenantId, string.Join(", ", request.Permissions));

        await _permissionService.RevokeTenantPermissionAsync(
            request.UserId,
            request.TenantId,
            request.Permissions);

        // Log the permission revocation for audit
        await _auditService.LogPermissionGrantedAsync(
            request.UserId,
            request.TenantId,
            null,
            "Revoke",
            request.Permissions,
            request.Reason);

        _logger.LogInformation("Successfully revoked tenant permissions for User:{UserId} in Tenant:{TenantId}",
            request.UserId, request.TenantId);

        return Unit.Value;
    }
}