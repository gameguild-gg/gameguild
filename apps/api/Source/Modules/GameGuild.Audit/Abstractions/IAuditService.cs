namespace GameGuild.Audit;

/// <summary>
/// Service for creating and managing audit logs
/// </summary>
public interface IAuditService
{
    Task LogAsync(CreateAuditLogRequest request);

    Task LogPermissionGrantAsync(Guid userId, string permissionName, string resourceType, string? resourceId, Guid? tenantId = null);

    Task LogPermissionDenyAsync(Guid? userId, string permissionName, string resourceType, string? resourceId, string reason, Guid? tenantId = null);

    Task LogAuthenticationAsync(string actionType, Guid? userId, bool success, string? errorMessage = null);

    Task LogAdminActionAsync(Guid userId, string actionType, string description, object? metadata = null);

    Task LogSecurityViolationAsync(string violationType, string description, Guid? userId = null, object? metadata = null);

    Task<List<AuditLog>> GetAuditLogsAsync(AuditLogQuery query);

    Task<int> GetAuditLogCountAsync(AuditLogQuery query);

    // Tenant-specific audit methods
    Task LogTenantOperationAsync(string actionType, Guid tenantId, Guid? userId = null, string? description = null, object? metadata = null, bool success = true);

    Task LogTenantIsolationBypassAsync(Guid userId, string reason, object? metadata = null);

    // Privacy audit methods
    Task LogPrivacyOperationAsync(string actionType, Guid userId, string? settingName = null, string? oldValue = null, string? newValue = null, Guid? tenantId = null, object? metadata = null);

    Task LogPrivacyViolationAsync(Guid? requestingUserId, Guid targetUserId, string attemptedField, string reason, Guid? tenantId = null);

    // Username normalization audit methods
    Task LogUsernameOperationAsync(string actionType, Guid userId, string? oldUsername = null, string? newUsername = null, string? reason = null, object? metadata = null);
}
