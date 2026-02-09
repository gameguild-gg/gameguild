using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Features;

/// <summary>
///     Handles permission-based and custom targeting rules.
/// </summary>
public sealed class CustomTargetingHandler : ITargetingRuleHandler
{
    public int Priority
    {
        get => 5; // Lowest priority - catch-all
    }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        // Handle custom attribute targeting
        var customTarget = featureFlag.Targets.FirstOrDefault(t => t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.Custom);

        if (customTarget != null && context.CustomAttributes is { Count: > 0 })
        {
            // Check if custom attributes match the target identifier pattern
            // Expected format: "key=value" or just match the identifier directly
            var targetIdentifier = customTarget.TargetIdentifier;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - Explicit null check for defensive coding
            var isMatch = targetIdentifier != null &&
                          context.CustomAttributes.Any(attr => $"{attr.Key}={attr.Value}" == targetIdentifier || attr.Value.ToString() == targetIdentifier);

            if (isMatch)
            {
                var isEnabled = customTarget.IsEnabled;

                // Apply target-specific rollout percentage if needed
                if (isEnabled && customTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
                {
                    var userId = context.UserId?.ToString() ?? string.Empty;
                    var identifier = $"{context.TenantId}:{userId}";
                    // targetIdentifier is confirmed non-null by the isMatch condition above
                    isEnabled = IsInRollout(identifier, customTarget.RolloutPercentage, targetIdentifier!);
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
