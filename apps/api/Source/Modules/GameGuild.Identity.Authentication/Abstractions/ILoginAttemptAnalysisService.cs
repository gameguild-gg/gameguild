namespace GameGuild.Identity.Authentication;

/// <summary>
///     Records login attempts, runs inline risk analysis, and persists results.
/// </summary>
public interface ILoginAttemptAnalysisService
{
    /// <summary>
    ///     Records a login attempt, runs inline risk analysis, and persists the result.
    /// </summary>
    Task<AuthenticationAttemptAnalysis> RecordLoginAttemptAsync(
        CreateAuthenticationAttemptRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    ///     Analyzes a full login-attempt context for anomalies (IP, user-agent, device, location).
    /// </summary>
    Task<AuthenticationAnomalyResult> AnalyzeLoginAttemptAsync(AuthenticationAttemptContext context);

    /// <summary>
    ///     Records a suspicious activity for future analysis and threat intelligence.
    /// </summary>
    Task RecordSuspiciousActivityAsync(SuspiciousActivity activity);
}
