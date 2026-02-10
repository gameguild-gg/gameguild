using GameGuild.CQRS.Models;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service interface for Permission Audit Logging
/// </summary>
public interface IPermissionAuditService
{
    Task<PermissionAuditLog> LogPermissionChangeAsync(
        PermissionOperationType operationType,
        Guid? userId,
        Guid performedBy,
        Guid? tenantId,
        string? permissionType = null,
        Guid? resourceId = null,
        string? resourceType = null,
        string? oldValue = null,
        string? newValue = null,
        string? reason = null,
        bool success = true,
        string? errorMessage = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetAuditLogsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? limit = null,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetUserAuditHistoryAsync(
        Guid userId,
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetResourceAuditHistoryAsync(
        Guid resourceId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetAuditLogsByOperationAsync(
        PermissionOperationType operationType,
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetFailedOperationsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
///     Repository interface for Permission Audit Logs
/// </summary>
public interface IPermissionAuditLogRepository
{
    Task<PermissionAuditLog> CreateAsync(
        PermissionAuditLog auditLog,
        CancellationToken cancellationToken = default
    );

    Task<PermissionAuditLog?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByTenantAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByUserAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByDateRangeAsync(
        DateTime fromDate,
        DateTime toDate,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByOperationTypeAsync(
        PermissionOperationType operationType,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByTenantAsync(
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByUserAsync(
        Guid userId,
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByPermissionAsync(
        string permission,
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );

    Task<List<PermissionAuditLog>> GetByResourceTypeAsync(
        string resourceType,
        Guid? tenantId,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default
    );
}

/// <summary>
///     Permission audit log entry
/// </summary>
public class PermissionAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public TenantId? TenantId { get; set; }

    public PermissionOperationType OperationType { get; set; }

    public Guid? UserId { get; set; } // User whose permissions were affected

    public Guid? ResourceId { get; set; }

    public string? ResourceType { get; set; }

    public string? PermissionType { get; set; }

    /// <summary>
    ///     Alias for PermissionType - used by analytics services
    /// </summary>
    public string? Permission => PermissionType;

    public string? PermissionDetails { get; set; } // JSON with permission details

    public string? OldValue { get; set; } // JSON with previous state

    public string? NewValue { get; set; } // JSON with new state

    public Guid PerformedBy { get; set; } // User who performed the action

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? Reason { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorMessage { get; set; }

    public DateTime Timestamp { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     Check if operation was successful
    /// </summary>
    public bool IsSuccessful() => Success;

    /// <summary>
    ///     Check if operation failed
    /// </summary>
    public bool IsFailed() => !Success;
}
