using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

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
        await _ruleRepository.DeleteAsync(ruleId, cancellationToken).ConfigureAwait(false);
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
        var rules = await _ruleRepository.GetActiveRulesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var violations = new List<SoDViolation>();

        foreach (var rule in rules)
        {
            var hasConflict = await CheckRuleViolationAsync(rule, userId, tenantId, cancellationToken).ConfigureAwait(false);

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
                await _violationRepository.CreateAsync(violation, cancellationToken).ConfigureAwait(false);
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
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken).ConfigureAwait(false);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.Resolve(resolvedBy, action, notes);
        await _violationRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

        return violation;
    }

    public async Task<SoDViolation> GrantExceptionAsync(
        Guid violationId,
        Guid approvedBy,
        string justification,
        CancellationToken cancellationToken = default
    )
    {
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken).ConfigureAwait(false);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.MarkAsException(approvedBy, justification);
        await _violationRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

        return violation;
    }

    public async Task<SoDViolation> AcknowledgeViolationAsync(
        Guid violationId,
        CancellationToken cancellationToken = default
    )
    {
        var violation = await _violationRepository.GetByIdAsync(violationId, cancellationToken).ConfigureAwait(false);

        if (violation == null)
            throw new InvalidOperationException($"Violation {violationId} not found");

        violation.Acknowledge();
        await _violationRepository.UpdateAsync(violation, cancellationToken).ConfigureAwait(false);

        return violation;
    }

    public async Task<int> ScanForViolationsAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default
    )
    {
        _logger.LogInformation("Scanning for SoD violations in tenant {TenantId}", tenantId);
        // PLANNED: Iterate all active SoD rules, load users with matching permission sets,
        // and call CheckRuleViolationAsync for each user. Requires ISoDRuleRepository.GetActiveRulesAsync.
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
        // PLANNED: Resolve the user's effective permissions and check if any pair matches
        // the SoD rule's conflicting permission sets. Requires IPermissionResolutionService.
        return Task.FromResult(false);
    }
}
