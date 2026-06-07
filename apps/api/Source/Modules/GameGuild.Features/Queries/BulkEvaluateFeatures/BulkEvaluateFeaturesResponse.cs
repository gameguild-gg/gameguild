namespace GameGuild.Features;

/// <summary>
///     Bulk feature flags evaluation response
/// </summary>
public class BulkEvaluateFeaturesResponse
{
    public Dictionary<string, FeatureEvaluationResult> Results { get; init; } = [];

    public DateTime EvaluatedAt { get; set; } = SystemClock.UtcNow;

    public string Environment { get; set; } = string.Empty;
}
