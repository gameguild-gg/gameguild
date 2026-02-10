using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for managing Just-in-Time (JIT) permission elevations
/// </summary>
public class JitElevationService(
    IJitElevationRequestRepository repository,
    IPermissionAuditService auditService,
    ILogger<JitElevationService> logger
) : IJitElevationService
{
    private readonly IPermissionAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<JitElevationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IJitElevationRequestRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<JitElevationRequest> RequestElevationAsync(
        Guid requesterId,
        Guid? tenantId,
        string permission,
        string justification,
        int durationMinutes,
        Guid? resourceId = null,
        string? resourceType = null,
        DateTime? startsAt = null,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation(
            "User {RequesterId} requesting JIT elevation for {Permission} (Duration: {Duration}min)",
            requesterId,
            permission,
            durationMinutes
        );

        var request = new JitElevationRequest
        {
            RequesterId = requesterId,
            TenantId = tenantId,
            Permission = permission,
            ResourceId = resourceId,
            ResourceType = resourceType,
            Justification = justification,
            DurationMinutes = durationMinutes,
            StartsAt = startsAt ?? SystemClock.UtcNow,
            ExpiresAt = (startsAt ?? SystemClock.UtcNow).AddMinutes(durationMinutes),
            Status = ElevationRequestStatus.Pending
        };

        var result = await _repository.CreateAsync(request, cancellationToken).ConfigureAwait(false);

        await _auditService.LogPermissionChangeAsync(
            PermissionOperationType.ElevateJIT,
            requesterId,
            requesterId,
            tenantId,
            permission,
            resourceId,
            resourceType,
            null,
            $"JIT Elevation Requested: {durationMinutes}min",
            justification,
            true,
            null,
            null,
            null,
            cancellationToken
        );

        return result;
    }

    public async Task<JitElevationRequest> ApproveRequestAsync(
        Guid requestId,
        Guid reviewerId,
        string? comments = null,
        CancellationToken cancellationToken = default
    )
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);

        if (request == null)
            throw new InvalidOperationException($"Elevation request {requestId} not found");

        request.Approve(reviewerId, comments);
        await _repository.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Reviewer {ReviewerId} approved elevation request {RequestId}",
            reviewerId,
            requestId
        );

        return request;
    }

    public async Task<JitElevationRequest> DenyRequestAsync(
        Guid requestId,
        Guid reviewerId,
        string comments,
        CancellationToken cancellationToken = default
    )
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);

        if (request == null)
            throw new InvalidOperationException($"Elevation request {requestId} not found");

        request.Deny(reviewerId, comments);
        await _repository.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Reviewer {ReviewerId} denied elevation request {RequestId}",
            reviewerId,
            requestId
        );

        return request;
    }

    public async Task<bool> RevokeElevationAsync(
        Guid requestId,
        Guid revokedBy,
        string reason,
        CancellationToken cancellationToken = default
    )
    {
        var request = await _repository.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);

        if (request == null) return false;

        request.Revoke(revokedBy, reason);
        await _repository.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Elevation {RequestId} revoked by {RevokedBy}",
            requestId,
            revokedBy
        );

        return true;
    }

    public async Task<JitElevationRequest?> GetRequestByIdAsync(
        Guid requestId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByIdAsync(requestId, cancellationToken);

    public async Task<List<JitElevationRequest>> GetPendingRequestsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetPendingRequestsAsync(tenantId, cancellationToken);

    public async Task<List<JitElevationRequest>> GetUserRequestsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByRequesterAsync(userId, tenantId, cancellationToken);

    public async Task<List<JitElevationRequest>> GetActiveElevationsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetActiveByUserAsync(userId, tenantId, cancellationToken);

    public async Task<bool> HasActiveElevationAsync(
        Guid userId,
        string permission,
        Guid? tenantId,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default
    )
    {
        var activeElevations = await _repository.GetActiveByUserAsync(userId, tenantId, cancellationToken).ConfigureAwait(false);

        return activeElevations.Any(e =>
            e.Permission == permission &&
            e.ResourceId == resourceId &&
            e.IsActive()
        );
    }

    public async Task<int> CleanupExpiredElevationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredRequests = await _repository.GetExpiredElevationsAsync(cancellationToken).ConfigureAwait(false);

        foreach (var request in expiredRequests)
        {
            request.MarkExpired();
            await _repository.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Marked {Count} elevations as expired", expiredRequests.Count);

        return expiredRequests.Count;
    }
}
