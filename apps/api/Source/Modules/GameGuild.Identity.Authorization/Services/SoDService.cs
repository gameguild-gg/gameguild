using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Service for managing Separation of Duties (SoD) rules
/// </summary>
public class SoDService(
    ISoDRuleRepository ruleRepository,
    ISoDViolationRepository violationRepository,
    ILogger<SoDService> logger,
    IPermissionQueryService? permissionQueryService = null,
    ITenantPermissionRepository? tenantPermissionRepository = null
) : ISoDService
{
    private readonly ILogger<SoDService> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ISoDRuleRepository _ruleRepository =
        ruleRepository ?? throw new ArgumentNullException(nameof(ruleRepository));

    private readonly ISoDViolationRepository _violationRepository =
        violationRepository ?? throw new ArgumentNullException(nameof(violationRepository));

    private readonly IPermissionQueryService? _permissionQueryService = permissionQueryService;

    private readonly ITenantPermissionRepository? _tenantPermissionRepository = tenantPermissionRepository;

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
            var hasConflict = await CheckRuleViolationWithPermissionsAsync(rule, userId, tenantId, cancellationToken).ConfigureAwait(false);

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

        if (!tenantId.HasValue || _tenantPermissionRepository is null)
            return 0;

        var rules = await _ruleRepository.GetActiveRulesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        if (rules.Count == 0)
            return 0;

        var tenantPermissions = await _tenantPermissionRepository.GetByTenantAsync(tenantId.Value, cancellationToken).ConfigureAwait(false);
        var userIds = tenantPermissions
            .Where(permission => permission.UserId.HasValue)
            .Where(permission => permission.IsActive)
            .Where(permission => !permission.IsExpired())
            .Select(permission => permission.UserId!.Value)
            .Distinct()
            .ToArray();

        var detectedCount = 0;

        foreach (var rule in rules)
        {
            foreach (var userId in userIds)
            {
                if (!await CheckRuleViolationWithPermissionsAsync(rule, userId, tenantId, cancellationToken).ConfigureAwait(false))
                    continue;

                var violation = new SoDViolation
                {
                    RuleId = rule.Id,
                    UserId = userId,
                    TenantId = tenantId,
                    ConflictingItems = rule.ConflictingPermissions,
                    Status = SoDViolationStatus.Active,
                    ViolationDetails = $"{rule.Name}: {rule.Description}"
                };

                await _violationRepository.CreateAsync(violation, cancellationToken).ConfigureAwait(false);
                rule.RecordViolation();
                await _ruleRepository.UpdateAsync(rule, cancellationToken).ConfigureAwait(false);
                detectedCount++;
            }
        }

        return detectedCount;
    }

    private async Task<bool> CheckRuleViolationWithPermissionsAsync(
        SoDRule rule,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken
    )
    {
        if (_permissionQueryService is null)
            return await CheckRuleViolationAsync(rule, userId, tenantId, cancellationToken).ConfigureAwait(false);

        var conflictingPermissions = ParseConflictingPermissions(rule.ConflictingPermissions);
        if (conflictingPermissions.Count < 2)
            return false;

        var effectivePermissions = await _permissionQueryService
            .GetEffectivePermissionsAsync(userId, tenantId, cancellationToken)
            .ConfigureAwait(false);

        return conflictingPermissions
            .All(permission => effectivePermissions.Contains(permission, StringComparer.OrdinalIgnoreCase));
    }

    private static Task<bool> CheckRuleViolationAsync(
        SoDRule rule,
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken
    )
    {
        _ = rule;
        _ = userId;
        _ = tenantId;
        _ = cancellationToken;
        return Task.FromResult(false);
    }

    private static IReadOnlyList<string> ParseConflictingPermissions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        try
        {
            var permissions = JsonSerializer.Deserialize<string[]>(raw);
            return permissions?
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Select(permission => permission.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];
        }
        catch (JsonException)
        {
            return raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(permission => !string.IsNullOrWhiteSpace(permission))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
