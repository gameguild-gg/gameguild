using GameGuild.Modules.Permissions.Entities;

namespace GameGuild.Modules.Permissions.Abstractions;

/// <summary>
/// Service for auditing permission operations
/// </summary>
public interface IPermissionAuditService
{
    /// <summary>
    /// Log a permission grant operation
    /// </summary>
    Task LogPermissionGrantedAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        string operation,
        PermissionType[] permissions,
        string? reason = null,
        string? contentTypeName = null,
        Dictionary<string, object>? metadata = null);

    /// <summary>
    /// Log a permission check operation
    /// </summary>
    Task LogPermissionCheckAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission,
        bool hasPermission,
        string? contentTypeName = null);

    /// <summary>
    /// Log a permission denied access attempt
    /// </summary>
    Task LogPermissionDeniedAsync(
        Guid? userId,
        Guid? tenantId,
        Guid? resourceId,
        PermissionType permission,
        string? reason = null,
        string? contentTypeName = null);

    /// <summary>
    /// Get audit logs for a user
    /// </summary>
    Task<IEnumerable<PermissionAuditLog>> GetUserAuditLogsAsync(
        Guid userId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100);

    /// <summary>
    /// Get audit logs for a tenant
    /// </summary>
    Task<IEnumerable<PermissionAuditLog>> GetTenantAuditLogsAsync(
        Guid tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100);

    /// <summary>
    /// Get audit logs for a resource
    /// </summary>
    Task<IEnumerable<PermissionAuditLog>> GetResourceAuditLogsAsync(
        Guid resourceId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100);

    /// <summary>
    /// Get failed permission attempts for security monitoring
    /// </summary>
    Task<IEnumerable<PermissionAuditLog>> GetFailedPermissionAttemptsAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int limit = 100);
}