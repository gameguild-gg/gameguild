namespace GameGuild.Features;

/// <summary>
///     Result of feature flag evaluation with detailed information
/// </summary>
public class FeatureEvaluationResult
{
    public string FeatureKey { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    public string? Value { get; set; }

    public string? Reason { get; set; }

    public int RolloutPercentage { get; set; }

    public bool IsTargeted { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public Dictionary<string, object> Metadata { get; init; } = [];

    public DateTime EvaluatedAt { get; set; } = SystemClock.UtcNow;
}
