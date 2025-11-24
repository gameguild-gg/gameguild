using System.Security.Cryptography;
using System.Text;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Configuration;
using GameGuild.Features.Entities;
using GameGuild.Features.Models;

namespace GameGuild.Features.Services.Handlers;

/// <summary>
///     Handles subscription plan-based targeting rules.
/// </summary>
public class PlanTargetingHandler : ITargetingRuleHandler
{
    public int Priority { get => 3; }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (context.SubscriptionPlanId == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var planTarget = featureFlag.Targets?.FirstOrDefault(t =>
            t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.Plan && t.TargetIdentifier.Equals(context.SubscriptionPlanId, StringComparison.OrdinalIgnoreCase)
        );

        if (planTarget == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var isEnabled = planTarget.IsEnabled;

        // Apply target-specific rollout percentage if needed
        if (isEnabled && planTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
        {
            var userId = context.UserId?.ToString() ?? string.Empty;
            var identifier = $"{context.TenantId}:{userId}";
            isEnabled = IsInRollout(identifier, planTarget.RolloutPercentage, planTarget.TargetIdentifier);
        }

        var value = isEnabled ? planTarget.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

        return Task.FromResult<FeatureEvaluationResult?>(
            new FeatureEvaluationResult
            {
                FeatureKey = featureFlag.Key,
                IsEnabled = isEnabled,
                Value = value,
                Reason = $"Plan {context.SubscriptionPlanId} is targeted",
                RolloutPercentage = planTarget.RolloutPercentage,
                IsTargeted = true,
                TargetType = planTarget.TargetType,
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
