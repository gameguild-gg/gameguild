namespace GameGuild.Identity.Authentication;

/// <summary>
///     Result of ABAC policy evaluation
/// </summary>
public abstract class AbacEvaluationResult
{
    /// <summary>
    ///     Decision (Allow, Deny, NotApplicable)
    /// </summary>
    public AbacDecision Decision { get; set; } = AbacDecision.NotApplicable;

    /// <summary>
    ///     List of policies that were matched and evaluated
    /// </summary>
    public List<AbacPolicyMatch> MatchedPolicies { get; set; } = new List<AbacPolicyMatch>();

    /// <summary>
    ///     Reason for the decision
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Additional obligations or conditions
    /// </summary>
    public List<string> Obligations { get; set; } = new List<string>();

    /// <summary>
    ///     Evaluation duration in milliseconds
    /// </summary>
    public long EvaluationDurationMs { get; set; }

    /// <summary>
    ///     Evaluation timestamp
    /// </summary>
    public DateTime EvaluatedAt { get; set; } = SystemClock.UtcNow;

    /// <summary>
    ///     Whether the decision allows access
    /// </summary>
    public bool IsAllowed { get => Decision == AbacDecision.Allow; }

    /// <summary>
    ///     Whether the decision denies access
    /// </summary>
    public bool IsDenied { get => Decision == AbacDecision.Deny; }

    /// <summary>
    ///     Whether no applicable policy was found
    /// </summary>
    public bool IsNotApplicable { get => Decision == AbacDecision.NotApplicable; }
}
