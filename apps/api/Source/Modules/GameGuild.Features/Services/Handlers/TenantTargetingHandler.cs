using System.Security.Cryptography;
using System.Text;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Configuration;
using GameGuild.Features.Entities;
using GameGuild.Features.Models;

namespace GameGuild.Features.Services.Handlers;

/// <summary>
///     Handles tenant-based targeting rules.
/// </summary>
public class TenantTargetingHandler : ITargetingRuleHandler
{
    public int Priority
    {
        get => 1; // Highest priority
    }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (context.TenantId == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var tenantTarget = featureFlag.Targets?.FirstOrDefault(t => t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.Tenant && t.TargetIdentifier == context.TenantId.ToString());

        if (tenantTarget == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

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
