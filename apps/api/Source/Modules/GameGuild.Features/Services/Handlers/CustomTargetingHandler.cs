using System.Security.Cryptography;
using System.Text;
using GameGuild.Features.Abstractions;
using GameGuild.Features.Configuration;
using GameGuild.Features.Entities;
using GameGuild.Features.Models;

namespace GameGuild.Features.Services.Handlers;

/// <summary>
///     Handles permission-based and custom targeting rules.
/// </summary>
public class CustomTargetingHandler : ITargetingRuleHandler
{
    public int Priority
    {
        get => 5; // Lowest priority - catch-all
    }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        // Handle custom attribute targeting
        var customTarget = featureFlag.Targets?.FirstOrDefault(t => t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.Custom);

        if (customTarget != null && context.CustomAttributes?.Any() == true)
        {
            // Check if custom attributes match the target identifier pattern
            // Expected format: "key=value" or just match the identifier directly
            var isMatch = customTarget.TargetIdentifier != null &&
                          context.CustomAttributes.Any(attr => $"{attr.Key}={attr.Value}" == customTarget.TargetIdentifier || attr.Value?.ToString() == customTarget.TargetIdentifier);

            if (isMatch)
            {
                var isEnabled = customTarget.IsEnabled;

                // Apply target-specific rollout percentage if needed
                if (isEnabled && customTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
                {
                    var userId = context.UserId?.ToString() ?? string.Empty;
                    var identifier = $"{context.TenantId}:{userId}";
                    isEnabled = IsInRollout(identifier, customTarget.RolloutPercentage, customTarget.TargetIdentifier ?? string.Empty);
                }

                var value = isEnabled ? customTarget.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

                return Task.FromResult<FeatureEvaluationResult?>(
                    new FeatureEvaluationResult
                    {
                        FeatureKey = featureFlag.Key,
                        IsEnabled = isEnabled,
                        Value = value,
                        Reason = "Custom attributes match",
                        RolloutPercentage = customTarget.RolloutPercentage,
                        IsTargeted = true,
                        TargetType = customTarget.TargetType,
                        EvaluatedAt = DateTime.UtcNow
                    }
                );
            }
        }

        return Task.FromResult<FeatureEvaluationResult?>(null);
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
