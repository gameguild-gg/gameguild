using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Features;

/// <summary>
///     Handles user-based targeting rules.
/// </summary>
public sealed class UserTargetingHandler : ITargetingRuleHandler
{
    public int Priority { get => 2; }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (context.UserId == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var userTarget = featureFlag.Targets.FirstOrDefault(t => t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.User && t.TargetIdentifier == context.UserId.ToString());

        if (userTarget == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var isEnabled = userTarget.IsEnabled;

        // Apply target-specific rollout percentage if needed
        if (isEnabled && userTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
        {
            var identifier = $"{context.TenantId}:{context.UserId}";
            isEnabled = IsInRollout(identifier, userTarget.RolloutPercentage, userTarget.TargetIdentifier);
        }

        var value = isEnabled ? userTarget.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

        return Task.FromResult<FeatureEvaluationResult?>(
            new FeatureEvaluationResult
            {
                FeatureKey = featureFlag.Key,
                IsEnabled = isEnabled,
                Value = value,
                Reason = $"User {context.UserId} is targeted",
                RolloutPercentage = userTarget.RolloutPercentage,
                IsTargeted = true,
                TargetType = userTarget.TargetType,
                EvaluatedAt = SystemClock.UtcNow
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
