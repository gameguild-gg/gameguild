namespace GameGuild.Features;

/// <summary>
///     Request for bulk evaluation of multiple feature flags
/// </summary>
public class BulkEvaluationRequest
{
    /// <summary>
    ///     List of feature keys to evaluate
    /// </summary>
    public List<string> FeatureKeys { get; set; } = new List<string>();

    /// <summary>
    ///     Evaluation context for all features
    /// </summary>
    public FeatureContext Context { get; set; } = new FeatureContext();
}
