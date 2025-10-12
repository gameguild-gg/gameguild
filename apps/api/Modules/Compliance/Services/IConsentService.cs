namespace GameGuild.Modules.Compliance.Services;

/// <summary>
/// Service for managing user consent tracking
/// </summary>
public interface IConsentService
{
    /// <summary>
    /// Records user consent for a policy
    /// </summary>
    Task<Result<UserConsentDto>> GiveConsentAsync(GiveConsentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws user consent
    /// </summary>
    Task<Result> WithdrawConsentAsync(Guid consentId, string? reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves user consent history
    /// </summary>
    Task<Result<List<UserConsentDto>>> GetUserConsentsAsync(Guid userId, Guid? tenantId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if user has valid consent for a policy
    /// </summary>
    Task<Result<bool>> HasValidConsentAsync(Guid userId, Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all consents for a policy
    /// </summary>
    Task<Result<List<UserConsentDto>>> GetPolicyConsentsAsync(Guid policyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a compliance audit event
    /// </summary>
    Task<Result> RecordAuditAsync(RecordAuditRequest request, CancellationToken cancellationToken = default);
}

public record GiveConsentRequest(
    Guid UserId,
    Guid PolicyId,
    Guid PolicyVersionId,
    Guid? TenantId,
    string IpAddress,
    string UserAgent,
    string ConsentMethod,
    string? Metadata);

public record RecordAuditRequest(
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
    string? Metadata,
    string? Regulation,
    string Severity);

public record UserConsentDto(
    Guid Id,
    Guid UserId,
    Guid PolicyId,
    Guid PolicyVersionId,
    Guid? TenantId,
    bool IsConsented,
    DateTime ConsentedAt,
    string IpAddress,
    string UserAgent,
    DateTime? WithdrawnAt,
    string? WithdrawalReason,
    DateTime? ExpiresAt,
    string ConsentMethod,
    string? Metadata);
