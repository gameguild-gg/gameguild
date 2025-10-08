namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service interface for Just-in-Time (JIT) permission elevation
/// Manages time-bound temporary permission grants with approval workflow
/// </summary>
public interface IJitElevationService
{
    /// <summary>
    /// Request a temporary permission elevation
    /// </summary>
    Task<JitElevationRequest> RequestElevationAsync(
        Guid requesterId,
        Guid? tenantId,
        PermissionType permission,
        string justification,
        int durationMinutes,
        string? resourceType = null,
        Guid? resourceId = null,
        DateTime? startsAt = null,
        bool requiresApproval = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Approve an elevation request
    /// </summary>
    Task<JitElevationRequest> ApproveElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? comments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deny an elevation request
    /// </summary>
    Task<JitElevationRequest> DenyElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? comments = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a pending elevation request
    /// </summary>
    Task<JitElevationRequest> CancelElevationAsync(
        Guid requestId,
        Guid requesterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually revoke an active elevation
    /// </summary>
    Task<bool> RevokeElevationAsync(
        Guid requestId,
        Guid reviewerId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get elevation request by ID
    /// </summary>
    Task<JitElevationRequest?> GetElevationRequestAsync(
        Guid requestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all elevation requests for a user
    /// </summary>
    Task<List<JitElevationRequest>> GetUserElevationRequestsAsync(
        Guid userId,
        bool activeOnly = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending elevation requests for approval
    /// </summary>
    Task<List<JitElevationRequest>> GetPendingElevationRequestsAsync(
        Guid? tenantId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has an active elevation for a permission
    /// </summary>
    Task<bool> HasActiveElevationAsync(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-revoke expired elevations (background job)
    /// </summary>
    Task<int> AutoRevokeExpiredElevationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get elevation statistics for a tenant
    /// </summary>
    Task<ElevationStatistics> GetElevationStatisticsAsync(
        Guid? tenantId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics for elevation requests
/// </summary>
public class ElevationStatistics
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ApprovedRequests { get; set; }
    public int DeniedRequests { get; set; }
    public int ActiveElevations { get; set; }
    public int ExpiredElevations { get; set; }
    public int RevokedElevations { get; set; }
    public double AverageApprovalTimeMinutes { get; set; }
    public double AverageDurationMinutes { get; set; }
    public Dictionary<PermissionType, int> RequestsByPermission { get; set; } = new();
}
