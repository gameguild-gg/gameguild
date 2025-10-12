namespace GameGuild.Modules.Compliance.Services;

/// <summary>
/// Service for managing compliance policies and audit trails
/// </summary>
public interface IComplianceService
{
    /// <summary>
    /// Creates a new consent policy
    /// </summary>
    Task<Result<ConsentPolicyDto>> CreatePolicyAsync(CreatePolicyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing consent policy
    /// </summary>
    Task<Result<ConsentPolicyDto>> UpdatePolicyAsync(Guid policyId, UpdatePolicyRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a policy version making it the current version
    /// </summary>
    Task<Result> PublishPolicyAsync(Guid policyId, Guid versionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a consent policy
    /// </summary>
    Task<Result> DeactivatePolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new version of a policy
    /// </summary>
    Task<Result<PolicyVersionDto>> CreatePolicyVersionAsync(Guid policyId, CreateVersionRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a policy by ID
    /// </summary>
    Task<Result<ConsentPolicyDto>> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all policies for a tenant
    /// </summary>
    Task<Result<List<ConsentPolicyDto>>> GetPoliciesAsync(Guid? tenantId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit log entries
    /// </summary>
    Task<Result<List<ComplianceAuditDto>>> GetAuditLogAsync(AuditLogRequest request, CancellationToken cancellationToken = default);
}

public record CreatePolicyRequest(
    Guid? TenantId,
    string Name,
    string Type,
    string Description,
    bool RequiresConsent);

public record UpdatePolicyRequest(
    string? Name,
    string? Description,
    bool? RequiresConsent);

public record CreateVersionRequest(
    string VersionNumber,
    string Content,
    string ContentType,
    string? ChangeLog,
    DateTime EffectiveDate,
    DateTime? ExpiresAt,
    Guid CreatedByUserId);

public record AuditLogRequest(
    Guid? TenantId,
    Guid? UserId,
    string? EventType,
    DateTime? StartDate,
    DateTime? EndDate,
    int Skip = 0,
    int Take = 50);

public record ConsentPolicyDto(
    Guid Id,
    Guid? TenantId,
    string Name,
    string Type,
    string Description,
    bool IsActive,
    bool RequiresConsent,
    Guid? CurrentVersionId,
    DateTime? PublishedAt,
    DateTime CreatedAt);

public record PolicyVersionDto(
    Guid Id,
    Guid PolicyId,
    string VersionNumber,
    string Content,
    string ContentType,
    string? ChangeLog,
    DateTime EffectiveDate,
    DateTime? ExpiresAt,
    bool IsCurrent,
    Guid CreatedByUserId,
    DateTime CreatedAt);

public record ComplianceAuditDto(
    Guid Id,
    Guid? TenantId,
    Guid? UserId,
    string EventType,
    string EntityType,
    Guid? EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    string? IpAddress,
    string? UserAgent,
    DateTime OccurredAt,
    string? Metadata,
    string? Regulation,
    string Severity);
