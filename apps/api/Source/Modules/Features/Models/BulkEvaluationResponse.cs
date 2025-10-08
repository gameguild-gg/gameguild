namespace GameGuild.Modules.Features.Models;

/// <summary>
///     Bulk feature flags evaluation response
/// </summary>
public class BulkEvaluationResponse
{
    public Dictionary<string, FeatureEvaluationResult> Results { get; init; } = new Dictionary<string, FeatureEvaluationResult>();

    public DateTime EvaluatedAt { get; set; } = DateTime.UtcNow;

    public string Environment { get; set; } = string.Empty;
}

