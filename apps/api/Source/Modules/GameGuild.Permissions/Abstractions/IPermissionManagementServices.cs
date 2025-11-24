using GameGuild.Permissions.Domain.Entities;
using GameGuild.Permissions.Domain.Models;

namespace GameGuild.Permissions.Domain.Abstractions;

/// <summary>
///     Service interface for Just-in-Time (JIT) Permission Elevation
/// </summary>
public interface IJitElevationService
{
    Task<JitElevationRequest> RequestElevationAsync(
        Guid requesterId,
        Guid? tenantId,
        string permission,
        string justification,
        int durationMinutes,
        Guid? resourceId = null,
        string? resourceType = null,
        DateTime? startsAt = null,
        CancellationToken cancellationToken = default
    );

    Task<JitElevationRequest> ApproveRequestAsync(Guid requestId, Guid reviewerId, string? comments = null, CancellationToken cancellationToken = default);

    Task<JitElevationRequest> DenyRequestAsync(Guid requestId, Guid reviewerId, string comments, CancellationToken cancellationToken = default);

    Task<bool> RevokeElevationAsync(Guid requestId, Guid revokedBy, string reason, CancellationToken cancellationToken = default);

    Task<JitElevationRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetPendingRequestsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetUserRequestsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<JitElevationRequest>> GetActiveElevationsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> HasActiveElevationAsync(Guid userId, string permission, Guid? tenantId, Guid? resourceId = null, CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredElevationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Permission Delegation
/// </summary>
public interface IPermissionDelegationService
{
    Task<PermissionDelegation> DelegatePermissionsAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        string[ ] permissions,
        Guid? tenantId,
        Guid? resourceId = null,
        DateTime? expiresAt = null,
        bool canSubDelegate = false,
        string? reason = null,
        int? usageLimit = null,
        CancellationToken cancellationToken = default
    );

    Task<bool> RevokeDelegationAsync(Guid delegationId, CancellationToken cancellationToken = default);

    Task<PermissionDelegation?> GetDelegationByIdAsync(Guid delegationId, CancellationToken cancellationToken = default);

    Task<List<PermissionDelegation>> GetActiveDelegationsAsync(Guid delegateUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<PermissionDelegation>> GetDelegationsByDelegatorAsync(Guid delegatorUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> CheckDelegatedPermissionAsync(Guid delegateUserId, string permission, Guid? tenantId, Guid? resourceId = null, CancellationToken cancellationToken = default);

    Task<bool> RecordDelegationUsageAsync(Guid delegationId, CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredDelegationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Separation of Duties (SoD)
/// </summary>
public interface ISoDService
{
    Task<SoDRule> CreateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default);

    Task<SoDRule> UpdateRuleAsync(SoDRule rule, CancellationToken cancellationToken = default);

    Task<bool> DeleteRuleAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<SoDRule?> GetRuleByIdAsync(Guid ruleId, CancellationToken cancellationToken = default);

    Task<List<SoDRule>> GetRulesForTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDRule>> GetActiveRulesAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDViolation>> DetectViolationsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDViolation>> GetViolationsForUserAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<SoDViolation>> GetActiveViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default);

    Task<SoDViolation> ResolveViolationAsync(Guid violationId, Guid resolvedBy, SoDResolutionAction action, string notes, CancellationToken cancellationToken = default);

    Task<SoDViolation> GrantExceptionAsync(Guid violationId, Guid approvedBy, string justification, CancellationToken cancellationToken = default);

    Task<SoDViolation> AcknowledgeViolationAsync(Guid violationId, CancellationToken cancellationToken = default);

    Task<int> ScanForViolationsAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Delegated Administration
/// </summary>
public interface IDelegatedAdminService
{
    Task<DelegatedAdminScope> GrantDelegatedAdminAsync(DelegatedAdminScope scope, CancellationToken cancellationToken = default);

    Task<bool> RevokeDelegatedAdminAsync(Guid scopeId, CancellationToken cancellationToken = default);

    Task<DelegatedAdminScope?> GetScopeByIdAsync(Guid scopeId, CancellationToken cancellationToken = default);

    Task<List<DelegatedAdminScope>> GetAdminScopesAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetManagedUsersAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<List<string>> GetManagedResourceTypesAsync(Guid adminUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> CanManageUserAsync(Guid adminUserId, Guid targetUserId, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<bool> CanManageResourceAsync(Guid adminUserId, string resourceType, Guid? tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
///     Service interface for Permission Analytics
/// </summary>
public interface IPermissionAnalyticsService
{
    Task<List<PermissionUsageMetrics>> GetPermissionUsageAsync(Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<UserActivitySummary>> GetUserActivityAsync(Guid? tenantId, int top = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<ResourceAccessPattern>> GetResourceAccessPatternsAsync(Guid? tenantId, int top = 10, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<PermissionTrend>> GetPermissionTrendsAsync(Guid? tenantId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    Task<List<PermissionAnomaly>> DetectAnomaliesAsync(Guid? tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default);

    Task<PermissionAnalyticsReport> GenerateReportAsync(Guid? tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);
}

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

    Task<List<PermissionAuditLog>> GetAuditLogsAsync(Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetUserAuditHistoryAsync(Guid userId, Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetResourceAuditHistoryAsync(Guid resourceId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetAuditLogsByOperationAsync(PermissionOperationType operationType, Guid? tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    Task<List<PermissionAuditLog>> GetFailedOperationsAsync(Guid? tenantId, DateTime? fromDate = null, CancellationToken cancellationToken = default);
}
