using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Permissions.Abstractions;
using GameGuild.Modules.Permissions.Entities;


namespace GameGuild.Modules.Permissions.Services;

/// <summary>
/// Service for managing and evaluating conditional policies
/// </summary>
public class ConditionalPolicyService : IConditionalPolicyService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConditionalPolicyService> _logger;

    public ConditionalPolicyService(
        ApplicationDbContext context,
        ILogger<ConditionalPolicyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PolicyEvaluationResult>> EvaluatePoliciesAsync(
        Guid userId,
        Guid? tenantId,
        PermissionType permission,
        string? resourceType,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all applicable policies sorted by priority
            var policies = await _context.Set<ConditionalPolicy>()
                .Where(p => p.IsEnabled &&
                           (p.TenantId == tenantId || p.TenantId == null) &&
                           (p.PermissionType == null || p.PermissionType == permission) &&
                           (p.ResourceType == null || p.ResourceType == resourceType) &&
                           (p.EffectiveFrom == null || p.EffectiveFrom <= DateTime.UtcNow) &&
                           (p.EffectiveUntil == null || p.EffectiveUntil >= DateTime.UtcNow) &&
                           p.DeletedAt == null)
                .OrderByDescending(p => p.Priority)
                .ToListAsync(cancellationToken);

            var result = new PolicyEvaluationResult
            {
                Decision = PolicyDecision.Allow // Default to allow if no policies match
            };

            foreach (var policy in policies)
            {
                var matches = await EvaluatePolicyConditionsAsync(policy, context, cancellationToken);

                if (matches)
                {
                    result.MatchedPolicies.Add(new MatchedPolicy
                    {
                        PolicyId = policy.Id,
                        PolicyName = policy.Name,
                        Action = policy.Action,
                        Priority = policy.Priority,
                        Reason = policy.EnforcementMessage
                    });

                    // Apply policy action
                    switch (policy.Action)
                    {
                        case PolicyAction.Deny:
                            result.Decision = PolicyDecision.Deny;
                            result.Message = policy.EnforcementMessage ?? "Access denied by conditional policy";
                            _logger.LogWarning("Policy {PolicyId} denied access for user {UserId}", policy.Id, userId);
                            return Result<PolicyEvaluationResult>.Success(result); // Deny takes precedence

                        case PolicyAction.Require2FA:
                            result.Decision = PolicyDecision.Conditional;
                            result.Require2FA = true;
                            result.Message = policy.EnforcementMessage ?? "Additional authentication required";
                            break;

                        case PolicyAction.RequireApproval:
                            result.Decision = PolicyDecision.Conditional;
                            result.RequireApproval = true;
                            result.Message = policy.EnforcementMessage ?? "Approval required";
                            break;

                        case PolicyAction.Allow:
                            // Explicit allow
                            break;

                        case PolicyAction.LogOnly:
                            _logger.LogInformation("Policy {PolicyId} matched for user {UserId} (log only)",
                                policy.Id, userId);
                            break;

                        case PolicyAction.Challenge:
                            result.Decision = PolicyDecision.Conditional;
                            result.Message = policy.EnforcementMessage ?? "Additional verification required";
                            break;
                    }
                }
            }

            return Result<PolicyEvaluationResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating conditional policies for user {UserId}", userId);
            return Result<PolicyEvaluationResult>.Failure("Failed to evaluate conditional policies");
        }
    }

    public async Task<Result<ConditionalPolicy>> CreatePolicyAsync(
        ConditionalPolicy policy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate policy
            if (string.IsNullOrWhiteSpace(policy.Name))
                return Result<ConditionalPolicy>.Failure("Policy name is required");

            policy.CreatedAt = DateTime.UtcNow;
            policy.UpdatedAt = DateTime.UtcNow;

            _context.Set<ConditionalPolicy>().Add(policy);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Created conditional policy {PolicyId}: {PolicyName}", policy.Id, policy.Name);
            return Result<ConditionalPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conditional policy");
            return Result<ConditionalPolicy>.Failure("Failed to create conditional policy");
        }
    }

    public async Task<Result<ConditionalPolicy>> UpdatePolicyAsync(
        ConditionalPolicy policy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var existing = await _context.Set<ConditionalPolicy>()
                .FirstOrDefaultAsync(p => p.Id == policy.Id && p.DeletedAt == null, cancellationToken);

            if (existing == null)
                return Result<ConditionalPolicy>.Failure("Policy not found");

            // Update properties
            existing.Name = policy.Name;
            existing.Description = policy.Description;
            existing.ConditionType = policy.ConditionType;
            existing.PermissionType = policy.PermissionType;
            existing.ResourceType = policy.ResourceType;
            existing.Action = policy.Action;
            existing.Priority = policy.Priority;
            existing.IsEnabled = policy.IsEnabled;
            existing.TimeConditions = policy.TimeConditions;
            existing.EnvironmentConditions = policy.EnvironmentConditions;
            existing.LocationConditions = policy.LocationConditions;
            existing.DeviceConditions = policy.DeviceConditions;
            existing.CustomConditions = policy.CustomConditions;
            existing.EnforcementMessage = policy.EnforcementMessage;
            existing.EffectiveFrom = policy.EffectiveFrom;
            existing.EffectiveUntil = policy.EffectiveUntil;
            existing.UpdatedBy = policy.UpdatedBy;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated conditional policy {PolicyId}", policy.Id);
            return Result<ConditionalPolicy>.Success(existing);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating conditional policy {PolicyId}", policy.Id);
            return Result<ConditionalPolicy>.Failure("Failed to update conditional policy");
        }
    }

    public async Task<Result> DeletePolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _context.Set<ConditionalPolicy>()
                .FirstOrDefaultAsync(p => p.Id == policyId && p.DeletedAt == null, cancellationToken);

            if (policy == null)
                return Result.Failure("Policy not found");

            policy.DeletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deleted conditional policy {PolicyId}", policyId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting conditional policy {PolicyId}", policyId);
            return Result.Failure("Failed to delete conditional policy");
        }
    }

    public async Task<Result<ConditionalPolicy>> GetPolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _context.Set<ConditionalPolicy>()
                .FirstOrDefaultAsync(p => p.Id == policyId && p.DeletedAt == null, cancellationToken);

            if (policy == null)
                return Result<ConditionalPolicy>.Failure("Policy not found");

            return Result<ConditionalPolicy>.Success(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conditional policy {PolicyId}", policyId);
            return Result<ConditionalPolicy>.Failure("Failed to get conditional policy");
        }
    }

    public async Task<Result<List<ConditionalPolicy>>> ListPoliciesAsync(
        Guid? tenantId,
        bool includeDisabled = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<ConditionalPolicy>()
                .Where(p => (p.TenantId == tenantId || p.TenantId == null) && p.DeletedAt == null);

            if (!includeDisabled)
                query = query.Where(p => p.IsEnabled);

            var policies = await query
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToListAsync(cancellationToken);

            return Result<List<ConditionalPolicy>>.Success(policies);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing conditional policies for tenant {TenantId}", tenantId);
            return Result<List<ConditionalPolicy>>.Failure("Failed to list conditional policies");
        }
    }

    public async Task<Result<PolicyTestResult>> TestPolicyAsync(
        Guid policyId,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var policy = await _context.Set<ConditionalPolicy>()
                .FirstOrDefaultAsync(p => p.Id == policyId && p.DeletedAt == null, cancellationToken);

            if (policy == null)
                return Result<PolicyTestResult>.Failure("Policy not found");

            var matches = await EvaluatePolicyConditionsAsync(policy, context, cancellationToken);

            var result = new PolicyTestResult
            {
                Matches = matches,
                Action = policy.Action,
                Message = matches ? policy.EnforcementMessage : "Policy conditions did not match"
            };

            // Add detailed condition evaluation results
            await PopulateTestResultDetailsAsync(policy, context, result, cancellationToken);

            return Result<PolicyTestResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing conditional policy {PolicyId}", policyId);
            return Result<PolicyTestResult>.Failure("Failed to test conditional policy");
        }
    }

    public async Task<Result<PolicyStatistics>> GetStatisticsAsync(
        Guid? tenantId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Set<ConditionalPolicy>()
                .Where(p => (p.TenantId == tenantId || p.TenantId == null) && p.DeletedAt == null);

            var policies = await query.ToListAsync(cancellationToken);

            var stats = new PolicyStatistics
            {
                TotalPolicies = policies.Count,
                EnabledPolicies = policies.Count(p => p.IsEnabled)
            };

            // Group by condition type
            stats.ConditionTypeCounts = policies
                .GroupBy(p => p.ConditionType)
                .ToDictionary(g => g.Key, g => g.Count());

            return Result<PolicyStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting policy statistics for tenant {TenantId}", tenantId);
            return Result<PolicyStatistics>.Failure("Failed to get policy statistics");
        }
    }

    private async Task<bool> EvaluatePolicyConditionsAsync(
        ConditionalPolicy policy,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        return policy.ConditionType switch
        {
            PolicyConditionType.Time => EvaluateTimeConditions(policy.TimeConditions, context),
            PolicyConditionType.Environment => EvaluateEnvironmentConditions(policy.EnvironmentConditions, context),
            PolicyConditionType.Location => EvaluateLocationConditions(policy.LocationConditions, context),
            PolicyConditionType.Device => EvaluateDeviceConditions(policy.DeviceConditions, context),
            PolicyConditionType.Risk => EvaluateRiskConditions(policy.CustomConditions, context),
            PolicyConditionType.Composite => await EvaluateCompositeConditionsAsync(policy, context, cancellationToken),
            PolicyConditionType.Custom => EvaluateCustomConditions(policy.CustomConditions, context),
            _ => false
        };
    }

    private bool EvaluateTimeConditions(string? timeConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(timeConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<TimeConditions>(timeConditionsJson);
            if (conditions == null) return true;

            var requestTime = context.RequestTime;

            // Check day of week
            if (conditions.DaysOfWeek?.Any() == true &&
                !conditions.DaysOfWeek.Contains(requestTime.DayOfWeek))
                return false;

            // Check time ranges
            if (conditions.TimeRanges?.Any() == true)
            {
                var currentTime = requestTime.TimeOfDay;
                bool inRange = conditions.TimeRanges.Any(range =>
                    currentTime >= range.StartTime && currentTime <= range.EndTime);

                if (!inRange) return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating time conditions");
            return false;
        }
    }

    private bool EvaluateEnvironmentConditions(string? envConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(envConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<EnvironmentConditions>(envConditionsJson);
            if (conditions == null) return true;

            // Check environment
            if (conditions.Environments?.Any() == true &&
                !string.IsNullOrWhiteSpace(context.Environment) &&
                !conditions.Environments.Contains(context.Environment, StringComparer.OrdinalIgnoreCase))
                return false;

            // Check IP ranges
            if (conditions.IpRanges?.Any() == true &&
                !string.IsNullOrWhiteSpace(context.IpAddress))
            {
                // Simple IP range check (can be enhanced with proper CIDR matching)
                bool inRange = conditions.IpRanges.Any(range =>
                    context.IpAddress.StartsWith(range, StringComparison.OrdinalIgnoreCase));

                if (!inRange) return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating environment conditions");
            return false;
        }
    }

    private bool EvaluateLocationConditions(string? locationConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(locationConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<LocationConditions>(locationConditionsJson);
            if (conditions == null) return true;

            // Check allowed countries
            if (conditions.AllowedCountries?.Any() == true &&
                !string.IsNullOrWhiteSpace(context.Country) &&
                !conditions.AllowedCountries.Contains(context.Country, StringComparer.OrdinalIgnoreCase))
                return false;

            // Check denied countries
            if (conditions.DeniedCountries?.Any() == true &&
                !string.IsNullOrWhiteSpace(context.Country) &&
                conditions.DeniedCountries.Contains(context.Country, StringComparer.OrdinalIgnoreCase))
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating location conditions");
            return false;
        }
    }

    private bool EvaluateDeviceConditions(string? deviceConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(deviceConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<DeviceConditions>(deviceConditionsJson);
            if (conditions == null) return true;

            // Check device type
            if (conditions.AllowedDeviceTypes?.Any() == true &&
                !string.IsNullOrWhiteSpace(context.DeviceType) &&
                !conditions.AllowedDeviceTypes.Contains(context.DeviceType, StringComparer.OrdinalIgnoreCase))
                return false;

            // Check compliance
            if (conditions.RequireCompliancy && !context.IsDeviceCompliant)
                return false;

            // Check encryption
            if (conditions.RequireEncryption && !context.IsDeviceEncrypted)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating device conditions");
            return false;
        }
    }

    private bool EvaluateRiskConditions(string? customConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(customConditionsJson))
            return true;

        try
        {
            var conditions = JsonSerializer.Deserialize<RiskConditions>(customConditionsJson);
            if (conditions == null) return true;

            // Check risk score threshold
            if (conditions.MaxRiskScore.HasValue &&
                context.RiskScore.HasValue &&
                context.RiskScore.Value > conditions.MaxRiskScore.Value)
                return false;

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating risk conditions");
            return false;
        }
    }

    private async Task<bool> EvaluateCompositeConditionsAsync(
        ConditionalPolicy policy,
        PolicyEvaluationContext context,
        CancellationToken cancellationToken)
    {
        // Composite conditions require all condition types to match
        var timeMatch = EvaluateTimeConditions(policy.TimeConditions, context);
        var envMatch = EvaluateEnvironmentConditions(policy.EnvironmentConditions, context);
        var locationMatch = EvaluateLocationConditions(policy.LocationConditions, context);
        var deviceMatch = EvaluateDeviceConditions(policy.DeviceConditions, context);

        return timeMatch && envMatch && locationMatch && deviceMatch;
    }

    private bool EvaluateCustomConditions(string? customConditionsJson, PolicyEvaluationContext context)
    {
        if (string.IsNullOrWhiteSpace(customConditionsJson))
            return true;

        try
        {
            // Custom conditions can be extended by implementation
            var conditions = JsonSerializer.Deserialize<Dictionary<string, object>>(customConditionsJson);
            if (conditions == null) return true;

            // Default to true for custom conditions (can be enhanced)
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error evaluating custom conditions");
            return false;
        }
    }

    private async Task PopulateTestResultDetailsAsync(
        ConditionalPolicy policy,
        PolicyEvaluationContext context,
        PolicyTestResult result,
        CancellationToken cancellationToken)
    {
        // Add details about which conditions matched/didn't match
        if (policy.TimeConditions != null)
        {
            var matches = EvaluateTimeConditions(policy.TimeConditions, context);
            (matches ? result.MatchedConditions : result.UnmatchedConditions).Add("Time conditions");
        }

        if (policy.EnvironmentConditions != null)
        {
            var matches = EvaluateEnvironmentConditions(policy.EnvironmentConditions, context);
            (matches ? result.MatchedConditions : result.UnmatchedConditions).Add("Environment conditions");
        }

        if (policy.LocationConditions != null)
        {
            var matches = EvaluateLocationConditions(policy.LocationConditions, context);
            (matches ? result.MatchedConditions : result.UnmatchedConditions).Add("Location conditions");
        }

        if (policy.DeviceConditions != null)
        {
            var matches = EvaluateDeviceConditions(policy.DeviceConditions, context);
            (matches ? result.MatchedConditions : result.UnmatchedConditions).Add("Device conditions");
        }

        await Task.CompletedTask;
    }
}

// Condition models for JSON serialization
internal class TimeConditions
{
    public List<DayOfWeek>? DaysOfWeek { get; set; }
    public List<TimeRange>? TimeRanges { get; set; }
    public string? TimeZone { get; set; }
}

internal class TimeRange
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

internal class EnvironmentConditions
{
    public List<string>? Environments { get; set; }
    public List<string>? IpRanges { get; set; }
}

internal class LocationConditions
{
    public List<string>? AllowedCountries { get; set; }
    public List<string>? DeniedCountries { get; set; }
    public List<string>? AllowedRegions { get; set; }
    public List<string>? DeniedRegions { get; set; }
}

internal class DeviceConditions
{
    public List<string>? AllowedDeviceTypes { get; set; }
    public bool RequireCompliancy { get; set; }
    public bool RequireEncryption { get; set; }
}

internal class RiskConditions
{
    public double? MaxRiskScore { get; set; }
}
