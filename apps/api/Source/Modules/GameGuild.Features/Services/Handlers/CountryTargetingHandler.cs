using System.Security.Cryptography;
using System.Text;

namespace GameGuild.Features;

/// <summary>
///     Handles country-based targeting rules.
/// </summary>
public sealed class CountryTargetingHandler : ITargetingRuleHandler
{
    public int Priority { get => 4; }

    public Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default)
    {
        if (context.Country == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var countryTarget = featureFlag.Targets.FirstOrDefault(t =>
            t.TargetType.ToLowerInvariant() == FeatureFlagConstants.TargetTypes.Country && t.TargetIdentifier.Equals(context.Country, StringComparison.OrdinalIgnoreCase)
        );

        if (countryTarget == null) { return Task.FromResult<FeatureEvaluationResult?>(null); }

        var isEnabled = countryTarget.IsEnabled;

        // Apply target-specific rollout percentage if needed
        if (isEnabled && countryTarget.RolloutPercentage < FeatureFlagConstants.MaxRolloutPercentage)
        {
            var userId = context.UserId?.ToString() ?? string.Empty;
            var identifier = $"{context.TenantId}:{userId}";
            isEnabled = IsInRollout(identifier, countryTarget.RolloutPercentage, countryTarget.TargetIdentifier);
        }

        var value = isEnabled ? countryTarget.CustomValue ?? featureFlag.EnabledValue : featureFlag.DefaultValue;

        return Task.FromResult<FeatureEvaluationResult?>(
            new FeatureEvaluationResult
            {
                FeatureKey = featureFlag.Key,
                IsEnabled = isEnabled,
                Value = value,
                Reason = $"Country {context.Country} is targeted",
                RolloutPercentage = countryTarget.RolloutPercentage,
                IsTargeted = true,
                TargetType = countryTarget.TargetType,
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
