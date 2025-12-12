namespace GameGuild.Features.Models;

/// <summary>
///     Bulk feature flags evaluation response
/// </summary>
public class BulkEvaluateFeaturesResponse
{
    public Dictionary<string, FeatureEvaluationResult> Results { get; init; } = [];

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    public string Environment { get; set; } = string.Empty;
}
