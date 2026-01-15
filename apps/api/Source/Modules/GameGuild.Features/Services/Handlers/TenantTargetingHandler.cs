using System.Security.Cryptography;
using System.Text;

using Microsoft.Extensions.Logging;

namespace GameGuild.Features;

/// <summary>
///     Handles tenant-based targeting rules.
///     Implements fail-closed behavior: if tenant targeting rules exist but no TenantId is provided,
///     the feature is disabled to prevent cross-tenant feature leakage.
/// </summary>
public class TenantTargetingHandler(ILogger<TenantTargetingHandler> logger) : ITargetingRuleHandler
{
    public int Priority
    {
        get => 1; // Highest priority
    }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        // Check if there are any tenant-specific targeting rules
        var hasTenantTargets = featureFlag.Targets.Any(t => 
            t.TargetType.Equals(FeatureFlagConstants.TargetTypes.Tenant, StringComparison.OrdinalIgnoreCase));

        if (!hasTenantTargets)
        {
            // No tenant targeting rules, let other handlers decide
            return Task.FromResult<FeatureEvaluationResult?>(null);
        }

        // FAIL-CLOSED: If tenant targeting rules exist but no TenantId provided, deny access
        if (context.TenantId == null)
        {
            logger.LogWarning(
                "Feature flag '{FeatureKey}' has tenant-targeting rules but no TenantId in context. " +
                "Applying fail-closed policy to prevent cross-tenant feature leakage.",
                featureFlag.Key);

            return Task.FromResult<FeatureEvaluationResult?>(
                new FeatureEvaluationResult
                {
                    FeatureKey = featureFlag.Key,
                    IsEnabled = false,
                    Value = featureFlag.DefaultValue,
                    Reason = "Fail-closed: Tenant targeting configured but no TenantId in context",
                    IsTargeted = true,
                    TargetType = FeatureFlagConstants.TargetTypes.Tenant,
                    EvaluatedAt = DateTime.UtcNow
                }
            );
        }

        var tenantTarget = featureFlag.Targets.FirstOrDefault(t => 
            t.TargetType.Equals(FeatureFlagConstants.TargetTypes.Tenant, StringComparison.OrdinalIgnoreCase) && 
            t.TargetIdentifier == context.TenantId.ToString());

        if (tenantTarget == null)
        {
            // Tenant has targeting rules but this tenant isn't targeted - check if should block or continue
            // Return null to let other handlers (like plan-based) potentially match
            return Task.FromResult<FeatureEvaluationResult?>(null);
        }

        var isEnabled = tenantTarget.IsEnabled;

        // Apply target-specific rollout percentage if needed
        if (isEnabled && tenantTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
        {
            var userId = context.UserId?.ToString() ?? string.Empty;
            var identifier = $"{context.TenantId}:{userId}";
            isEnabled = IsInRollout(identifier, tenantTarget.RolloutPercentage, tenantTarget.TargetIdentifier);
        }

        var value = isEnabled ? tenantTarget.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

        return Task.FromResult<FeatureEvaluationResult?>(
            new FeatureEvaluationResult
            {
                FeatureKey = featureFlag.Key,
                IsEnabled = isEnabled,
                Value = value,
                Reason = $"Tenant {context.TenantId} is targeted",
                RolloutPercentage = tenantTarget.RolloutPercentage,
                IsTargeted = true,
                TargetType = tenantTarget.TargetType,
                EvaluatedAt = DateTime.UtcNow
            }
        );
    }

    private static bool IsInRollout(string identifier, int percentage, string salt)
    {
        using var md5 = MD5.Create();
        var hashInput = $"{identifier}:{salt}";
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(hashInput));
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        var bucket = hashValue % 100;

        return bucket < percentage;
    }
}
