using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;

namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service implementation for Just-in-Time permission elevation
/// </summary>
public class JitElevationService : IJitElevationService
{
    private readonly ApplicationDbContext _context;
    private readonly IPermissionService _permissionService;
    private readonly IPermissionAuditService _auditService;
    private readonly ILogger<JitElevationService> _logger;

    public JitElevationService(
        ApplicationDbContext context,
        IPermissionService permissionService,
        IPermissionAuditService auditService,
        ILogger<JitElevationService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JitElevationRequest> RequestElevationAsync(
        Guid requesterId,
        Guid? tenantId,
        PermissionType permission,
        string justification,
        int durationMinutes,
        string? resourceType = null,
        Guid? resourceId = null,
        DateTime? startsAt = null,
        bool requiresApproval = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("User {RequesterId} requesting JIT elevation for {Permission} (Duration: {Duration}min)",
            requesterId, permission, durationMinutes);

        var request = new JitElevationRequest
        {
            RequesterId = requesterId,
            TenantId = tenantId,
            Permission = permission,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Justification = justification,
            DurationMinutes = durationMinutes,
            StartsAt = startsAt ?? DateTime.UtcNow,
            ExpiresAt = (startsAt ?? DateTime.UtcNow).AddMinutes(durationMinutes),
            Status = requiresApproval ? ElevationRequestStatus.Pending : ElevationRequestStatus.Approved,
            RequiresApproval = requiresApproval,
            Priority = 1
        };

        _context.Set<JitElevationRequest>().Add(request);
        await _context.SaveChangesAsync(cancellationToken);

        // Auto-grant if no approval required
        if (!requiresApproval)
        {
            await GrantElevationAsync(request, cancellationToken);
        }

        await _auditService.LogAdminActionAsync(
            requesterId,
            tenantId,
            "JIT_ELEVATION_REQUESTED",
            $"Requested {permission} elevation for {durationMinutes} minutes");

        return request;
    }

    public async Task<JitElevationRequest> ApproveElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<JitElevationRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Elevation request {requestId} not found");

        if (request.Status != ElevationRequestStatus.Pending)
            throw new InvalidOperationException($"Request is not in pending status (current: {request.Status})");

        request.Status = ElevationRequestStatus.Approved;
        request.ReviewerId = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewerComments = comments;

        await _context.SaveChangesAsync(cancellationToken);

        // Grant the permission
        await GrantElevationAsync(request, cancellationToken);

        await _auditService.LogAdminActionAsync(
            reviewerId,
            request.TenantId,
            "JIT_ELEVATION_APPROVED",
            $"Approved JIT elevation request {requestId} for user {request.RequesterId}");

        _logger.LogInformation("Reviewer {ReviewerId} approved elevation request {RequestId}", reviewerId, requestId);

        return request;
    }

    public async Task<JitElevationRequest> DenyElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? comments = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<JitElevationRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Elevation request {requestId} not found");

        if (request.Status != ElevationRequestStatus.Pending)
            throw new InvalidOperationException($"Request is not in pending status (current: {request.Status})");

        request.Status = ElevationRequestStatus.Denied;
        request.ReviewerId = reviewerId;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewerComments = comments;

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminActionAsync(
            reviewerId,
            request.TenantId,
            "JIT_ELEVATION_DENIED",
            $"Denied JIT elevation request {requestId} for user {request.RequesterId}. Reason: {comments}");

        _logger.LogInformation("Reviewer {ReviewerId} denied elevation request {RequestId}", reviewerId, requestId);

        return request;
    }

    public async Task<JitElevationRequest> CancelElevationAsync(
        Guid requestId,
        Guid requesterId,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<JitElevationRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Elevation request {requestId} not found");

        if (request.RequesterId != requesterId)
            throw new UnauthorizedAccessException("Only the requester can cancel their own request");

        if (request.Status != ElevationRequestStatus.Pending)
            throw new InvalidOperationException($"Can only cancel pending requests (current: {request.Status})");

        request.Status = ElevationRequestStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {RequesterId} cancelled elevation request {RequestId}", requesterId, requestId);

        return request;
    }

    public async Task<bool> RevokeElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var request = await _context.Set<JitElevationRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"Elevation request {requestId} not found");

        if (request.Status != ElevationRequestStatus.Granted)
            return false;

        request.Status = ElevationRequestStatus.Revoked;
        request.RevokedAt = DateTime.UtcNow;

        // Revoke the actual permission
        if (request.GrantedPermissionId.HasValue)
        {
            await _permissionService.RevokeTenantPermissionAsync(
                request.RequesterId,
                request.TenantId,
                new[] { request.Permission });
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _auditService.LogAdminActionAsync(
            reviewerId,
            request.TenantId,
            "JIT_ELEVATION_REVOKED",
            $"Manually revoked JIT elevation {requestId}. Reason: {reason}");

        _logger.LogInformation("Reviewer {ReviewerId} revoked elevation {RequestId}", reviewerId, requestId);

        return true;
    }

    public async Task<JitElevationRequest?> GetElevationRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<JitElevationRequest>()
            .FirstOrDefaultAsync(r => r.Id == requestId && !r.IsDeleted, cancellationToken);
    }

    public async Task<List<JitElevationRequest>> GetUserElevationRequestsAsync(
        Guid userId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<JitElevationRequest>()
            .Where(r => r.RequesterId == userId && !r.IsDeleted);

        if (activeOnly)
        {
            query = query.Where(r => r.Status == ElevationRequestStatus.Granted && r.ExpiresAt > DateTime.UtcNow);
        }

        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<JitElevationRequest>> GetPendingElevationRequestsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<JitElevationRequest>()
            .Where(r => r.Status == ElevationRequestStatus.Pending && !r.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(r => r.TenantId == tenantId);
        }

        return await query.OrderBy(r => r.Priority).ThenBy(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveElevationAsync(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<JitElevationRequest>()
            .Where(r => r.RequesterId == userId)
            .Where(r => r.TenantId == tenantId)
            .Where(r => r.Permission == permission)
            .Where(r => r.Status == ElevationRequestStatus.Granted)
            .Where(r => r.ExpiresAt > DateTime.UtcNow)
            .Where(r => !r.IsDeleted);

        if (resourceId.HasValue)
        {
            query = query.Where(r => r.ResourceId == resourceId);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<int> AutoRevokeExpiredElevationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredElevations = await _context.Set<JitElevationRequest>()
            .Where(r => r.Status == ElevationRequestStatus.Granted)
            .Where(r => r.AutoRevoke)
            .Where(r => r.ExpiresAt <= DateTime.UtcNow)
            .Where(r => !r.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var elevation in expiredElevations)
        {
            elevation.Status = ElevationRequestStatus.Expired;
            elevation.RevokedAt = DateTime.UtcNow;

            // Revoke the actual permission
            if (elevation.GrantedPermissionId.HasValue)
            {
                await _permissionService.RevokeTenantPermissionAsync(
                    elevation.RequesterId,
                    elevation.TenantId,
                    new[] { elevation.Permission });
            }
        }

        if (expiredElevations.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Auto-revoked {Count} expired JIT elevations", expiredElevations.Count);
        }

        return expiredElevations.Count;
    }

    public async Task<ElevationStatistics> GetElevationStatisticsAsync(
        Guid? tenantId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<JitElevationRequest>()
            .Where(r => !r.IsDeleted);

        if (tenantId.HasValue)
            query = query.Where(r => r.TenantId == tenantId);

        if (fromDate.HasValue)
            query = query.Where(r => r.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.CreatedAt <= toDate.Value);

        var requests = await query.ToListAsync(cancellationToken);

        var stats = new ElevationStatistics
        {
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == ElevationRequestStatus.Pending),
            ApprovedRequests = requests.Count(r => r.Status == ElevationRequestStatus.Approved || r.Status == ElevationRequestStatus.Granted),
            DeniedRequests = requests.Count(r => r.Status == ElevationRequestStatus.Denied),
            ActiveElevations = requests.Count(r => r.IsActive),
            ExpiredElevations = requests.Count(r => r.Status == ElevationRequestStatus.Expired),
            RevokedElevations = requests.Count(r => r.Status == ElevationRequestStatus.Revoked)
        };

        var approvedRequests = requests.Where(r => r.ReviewedAt.HasValue).ToList();
        if (approvedRequests.Any())
        {
            stats.AverageApprovalTimeMinutes = approvedRequests
                .Average(r => (r.ReviewedAt!.Value - r.CreatedAt).TotalMinutes);
        }

        if (requests.Any())
        {
            stats.AverageDurationMinutes = requests.Average(r => r.DurationMinutes);
        }

        stats.RequestsByPermission = requests
            .GroupBy(r => r.Permission)
            .ToDictionary(g => g.Key, g => g.Count());

        return stats;
    }

    private async Task GrantElevationAsync(JitElevationRequest request, CancellationToken cancellationToken)
    {
        request.Status = ElevationRequestStatus.Granted;
        request.GrantedAt = DateTime.UtcNow;

        // Grant the actual permission with expiration
        var permission = await _permissionService.GrantTenantPermissionAsync(
            request.RequesterId,
            request.TenantId,
            new[] { request.Permission });

        request.GrantedPermissionId = permission.Id;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Granted JIT elevation {RequestId} for user {UserId}, expires at {ExpiresAt}",
            request.Id, request.RequesterId, request.ExpiresAt);
    }
}
