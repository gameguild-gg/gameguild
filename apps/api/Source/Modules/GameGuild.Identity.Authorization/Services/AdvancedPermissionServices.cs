using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for managing Just-in-Time (JIT) permission elevations
/// </summary>
public class JitElevationService(
    IJitElevationRequestRepository repository,
    IPermissionService permissionService,
    IPermissionAuditService auditService,
    ILogger<JitElevationService> logger
) : IJitElevationService
{
    private readonly IPermissionAuditService _auditService =
        auditService ?? throw new ArgumentNullException(nameof(auditService));

    private readonly ILogger<JitElevationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPermissionService _permissionService =
        permissionService ?? throw new ArgumentNullException(nameof(permissionService));

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
            StartsAt = startsAt ?? DateTime.UtcNow,
            ExpiresAt = (startsAt ?? DateTime.UtcNow).AddMinutes(durationMinutes),
            Status = ElevationRequestStatus.Pending
        };

        var result = await _repository.CreateAsync(request, cancellationToken);

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
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);

        if (request == null)
            throw new InvalidOperationException($"Elevation request {requestId} not found");

        request.Approve(reviewerId, comments);
        await _repository.UpdateAsync(request, cancellationToken);

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
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);

        if (request == null)
            throw new InvalidOperationException($"Elevation request {requestId} not found");

        request.Deny(reviewerId, comments);
        await _repository.UpdateAsync(request, cancellationToken);

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
        var request = await _repository.GetByIdAsync(requestId, cancellationToken);

        if (request == null) return false;

        request.Revoke(revokedBy, reason);
        await _repository.UpdateAsync(request, cancellationToken);

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
        var activeElevations = await _repository.GetActiveByUserAsync(userId, tenantId, cancellationToken);

        return activeElevations.Any(e =>
            e.Permission == permission &&
            e.ResourceId == resourceId &&
            e.IsActive()
        );
    }

    public async Task<int> CleanupExpiredElevationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredRequests = await _repository.GetExpiredElevationsAsync(cancellationToken);

        foreach (var request in expiredRequests)
        {
            request.MarkExpired();
            await _repository.UpdateAsync(request, cancellationToken);
        }

        _logger.LogInformation("Marked {Count} elevations as expired", expiredRequests.Count);

        return expiredRequests.Count;
    }
}

/// <summary>
///     Service for managing permission delegations
/// </summary>
public class PermissionDelegationService(
    IPermissionDelegationRepository repository,
    ILogger<PermissionDelegationService> logger
) : IPermissionDelegationService
{
    private readonly ILogger<PermissionDelegationService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IPermissionDelegationRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<PermissionDelegation> DelegatePermissionsAsync(
        Guid delegatorUserId,
        Guid delegateUserId,
        string[] permissions,
        Guid? tenantId,
        Guid? resourceId = null,
        DateTime? expiresAt = null,
        bool canSubDelegate = false,
        string? reason = null,
        int? usageLimit = null,
        CancellationToken cancellationToken = default
    )
    {
        var delegation = new PermissionDelegation
        {
            DelegatorUserId = delegatorUserId,
            DelegateUserId = delegateUserId,
            DelegatedPermissions = permissions,
            TenantId = tenantId,
            ResourceId = resourceId,
            ExpiresAt = expiresAt,
            CanSubDelegate = canSubDelegate,
            Reason = reason,
            UsageLimit = usageLimit,
            IsActive = true
        };

        return await _repository.CreateAsync(delegation, cancellationToken);
    }

    public async Task<bool> RevokeDelegationAsync(
        Guid delegationId,
        CancellationToken cancellationToken = default
    )
    {
        var delegation = await _repository.GetByIdAsync(delegationId, cancellationToken);

        if (delegation == null) return false;

        delegation.Deactivate();
        await _repository.UpdateAsync(delegation, cancellationToken);

        return true;
    }

    public async Task<PermissionDelegation?> GetDelegationByIdAsync(
        Guid delegationId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByIdAsync(delegationId, cancellationToken);

    public async Task<List<PermissionDelegation>> GetActiveDelegationsAsync(
        Guid delegateUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetActiveByDelegateAsync(delegateUserId, tenantId, cancellationToken);

    public async Task<List<PermissionDelegation>> GetDelegationsByDelegatorAsync(
        Guid delegatorUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByDelegatorAsync(delegatorUserId, tenantId, cancellationToken);

    public async Task<bool> CheckDelegatedPermissionAsync(
        Guid delegateUserId,
        string permission,
        Guid? tenantId,
        Guid? resourceId = null,
        CancellationToken cancellationToken = default
    )
    {
        var delegations = await _repository.GetActiveByDelegateAsync(delegateUserId, tenantId, cancellationToken);

        return delegations.Any(d =>
            d.DelegatedPermissions.Contains(permission) &&
            d.IsValidNow() &&
            (d.ResourceId == null || d.ResourceId == resourceId)
        );
    }

    public async Task<bool> RecordDelegationUsageAsync(
        Guid delegationId,
        CancellationToken cancellationToken = default
    )
    {
        var delegation = await _repository.GetByIdAsync(delegationId, cancellationToken);

        if (delegation == null) return false;

        delegation.RecordUsage();
        await _repository.UpdateAsync(delegation, cancellationToken);

        return true;
    }

    public async Task<int> CleanupExpiredDelegationsAsync(CancellationToken cancellationToken = default)
    {
        var expiredDelegations = await _repository.GetExpiredDelegationsAsync(cancellationToken);

        foreach (var delegation in expiredDelegations)
        {
            delegation.Deactivate();
            await _repository.UpdateAsync(delegation, cancellationToken);
        }

        _logger.LogInformation("Cleaned up {Count} expired delegations", expiredDelegations.Count);

        return expiredDelegations.Count;
    }
}

/// <summary>
///     Service for managing Separation of Duties (SoD) rules
/// </summary>
public class SoDService(
    ISoDRuleRepository ruleRepository,
    ISoDViolationRepository violationRepository,
    ILogger<SoDService> logger
) : ISoDService
{
    private readonly ILogger<SoDService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ISoDRuleRepository _ruleRepository =
        ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));

    private readonly ISoDViolationRepository _violationRepository =
        violationRepository ?? throw new ArgumentNullException(nameof(violationRepository));

    public async Task<SoDRule> CreateRuleAsync(
        SoDRule rule,
        CancellationToken cancellationToken = default
    ) => await _ruleRepository.CreateAsync(rule, cancellationToken);

    public async Task<SoDRule> UpdateRuleAsync(
        SoDRule rule,
        CancellationToken cancellationToken = default
    ) => await _ruleRepository.UpdateAsync(rule, cancellationToken);

    public async Task<bool> DeleteRuleAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default
    )
    {
        await _ruleRepository.DeleteAsync(ruleId, cancellationToken);
        return true;
    }

    public async Task<SoDRule?> GetRuleByIdAsync(
        Guid ruleId,
        CancellationToken cancellationToken = default
    ) => await _ruleRepository.GetByIdAsync(ruleId, cancellationToken);

    public async Task<List<SoDRule>> GetRulesForTenantAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _ruleRepository.GetByTenantAsync(tenantId, cancellationToken);

    public async Task<List<SoDRule>> GetActiveRulesAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _ruleRepository.GetActiveRulesAsync(tenantId, cancellationToken);

    public async Task<List<SoDViolation>> DetectViolationsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var rules = await _ruleRepository.GetActiveRulesAsync(tenantId, cancellationToken);
        var violations = new List<SoDViolation>();

        foreach (var rule in rules)
        {
            var hasConflict = await CheckRuleViolationAsync(rule, userId, tenantId, cancellationToken);

            if (hasConflict)
            {
                var violation = new SoDViolation
                {
                    RuleId = rule.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    ConflictingItems = rule.ConflictingPermissions,
                    Status = SoDViolationStatus.Active,
                    ViolationDetails = $"{rule.Name}: {rule.Description}"
                };
                violations.Add(violation);
                await _violationRepository.CreateAsync(violation, cancellationToken);
            }
        }

        return violations;
    }

    public async Task<List<SoDViolation>> GetViolationsForUserAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _violationRepository.GetByUserAsync(userId, tenantId, cancellationToken);

    public async Task<List<SoDViolation>> GetActiveViolationsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _violationRepository.GetActiveViolationsAsync(tenantId, cancellationToken);

    public async Task<SoDViolation> ResolveViolationAsync(
        Guid violationId,
        Guid resolvedBy,
        SoDResolutionAction action,
        string notes,
        CancellationToken cancellationToken = default
    )
    {
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.Resolve(resolvedBy, action, notes);
        await _violationRepository.UpdateAsync(violation, cancellationToken);

        return violation;
    }

    public async Task<SoDViolation> GrantExceptionAsync(
        Guid violationId,
        Guid approvedBy,
        string justification,
        CancellationToken cancellationToken = default
    )
    {
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.MarkAsException(approvedBy, justification);
        await _violationRepository.UpdateAsync(violation, cancellationToken);

        return violation;
    }

    public async Task<SoDViolation> AcknowledgeViolationAsync(
        Guid violationId,
        CancellationToken cancellationToken = default
    )
    {
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.Acknowledge();
        await _violationRepository.UpdateAsync(violation, cancellationToken);

        return violation;
    }

    public async Task<int> ScanForViolationsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Scanning for SoD violations in tenant {TenantId}", tenantId);
        // TODO: Implement comprehensive scan across all users
        await Task.CompletedTask;
        return 0;
    }

    private static Task<bool> CheckRuleViolationAsync(
        SoDRule rule,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken
    )
    {
        // TODO: Implement actual permission checking logic
        return Task.FromResult(false);
    }
}

/// <summary>
///     Service for managing delegated administration scopes
/// </summary>
public class DelegatedAdminService(
    IDelegatedAdminScopeRepository repository,
    ILogger<DelegatedAdminService> logger
) : IDelegatedAdminService
{
    private readonly ILogger<DelegatedAdminService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly IDelegatedAdminScopeRepository _repository =
        repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<DelegatedAdminScope> GrantDelegatedAdminAsync(
        DelegatedAdminScope scope,
        CancellationToken cancellationToken = default
    ) => await _repository.CreateAsync(scope, cancellationToken);

    public async Task<bool> RevokeDelegatedAdminAsync(
        Guid scopeId,
        CancellationToken cancellationToken = default
    )
    {
        await _repository.DeleteAsync(scopeId, cancellationToken);
        return true;
    }

    public async Task<DelegatedAdminScope?> GetScopeByIdAsync(
        Guid scopeId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByIdAsync(scopeId, cancellationToken);

    public async Task<List<DelegatedAdminScope>> GetAdminScopesAsync(
        Guid adminUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    ) => await _repository.GetByAdminUserAsync(adminUserId, tenantId, cancellationToken);

    public async Task<List<Guid>> GetManagedUsersAsync(
        Guid adminUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var scopes = await _repository.GetByAdminUserAsync(adminUserId, tenantId, cancellationToken);
        // TODO: Parse AllowedUserIds JSON to extract Guid list
        return new List<Guid>();
    }

    public async Task<List<string>> GetManagedResourceTypesAsync(
        Guid adminUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var scopes = await _repository.GetByAdminUserAsync(adminUserId, tenantId, cancellationToken);
        // TODO: Parse AllowedResourceTypes JSON to extract resource type list
        return new List<string>();
    }

    public async Task<bool> CanManageUserAsync(
        Guid adminUserId,
        Guid targetUserId,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var managedUsers = await GetManagedUsersAsync(adminUserId, tenantId, cancellationToken);
        return managedUsers.Contains(targetUserId);
    }

    public async Task<bool> CanManageResourceAsync(
        Guid adminUserId,
        string resourceType,
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        var managedResourceTypes = await GetManagedResourceTypesAsync(adminUserId, tenantId, cancellationToken);
        return managedResourceTypes.Contains(resourceType);
    }
}
