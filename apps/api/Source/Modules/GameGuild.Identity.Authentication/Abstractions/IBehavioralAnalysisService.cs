namespace GameGuild.Identity.Authentication;

/// <summary>
///     Analyzes behavioral patterns to produce risk assessments based on historical user activity.
/// </summary>
public interface IBehavioralAnalysisService
{
    /// <summary>
    ///     Analyzes if a login attempt matches the user's typical behavioral patterns.
    /// </summary>
    Task<BehavioralAnalysisResult> AnalyzeBehavioralPatternsAsync(
        Guid userId,
        AuthenticationAttemptContext attemptContext);
}
