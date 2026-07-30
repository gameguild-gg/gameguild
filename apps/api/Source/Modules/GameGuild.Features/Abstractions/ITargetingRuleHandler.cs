namespace GameGuild.Features;

/// <summary>
///     Handler interface for the Chain of Responsibility pattern in targeting rule evaluation.
/// </summary>
public interface ITargetingRuleHandler
{
    /// <summary>
    ///     Priority of this handler (lower numbers = higher priority).
    /// </summary>
    int Priority { get; }

    /// <summary>
    ///     Evaluates targeting rules for this handler type.
    ///     Returns null if the handler doesn't match or can't decide.
    /// </summary>
    Task<FeatureEvaluationResult?> EvaluateAsync(FeatureFlag featureFlag, FeatureContext context, CancellationToken cancellationToken = default);
}
