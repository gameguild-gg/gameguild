namespace GameGuild.Features;

/// <summary>
///     Request DTO for bulk feature flags evaluation via HTTP API
/// </summary>
public sealed class BulkEvaluateFeaturesRequest
{
    /// <summary>
    ///     Feature flag keys to evaluate
    /// </summary>
    public required IEnumerable<string> FeatureKeys { get; set; }

    /// <summary>
    ///     Evaluation context for all features
    /// </summary>
    public FeatureContext Context { get; set; } = new FeatureContext();
}
