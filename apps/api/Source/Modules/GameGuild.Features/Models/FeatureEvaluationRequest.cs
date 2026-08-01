namespace GameGuild.Features;

/// <summary>
///     Request for evaluating a single feature flag
/// </summary>
public class FeatureEvaluationRequest
{
    /// <summary>
    ///     The feature flag key to evaluate
    /// </summary>
    public string FeatureKey { get; set; } = string.Empty;

    /// <summary>
    ///     Default value to return if evaluation fails
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    ///     Evaluation context
    /// </summary>
    public FeatureContext Context { get; set; } = new FeatureContext();
}
