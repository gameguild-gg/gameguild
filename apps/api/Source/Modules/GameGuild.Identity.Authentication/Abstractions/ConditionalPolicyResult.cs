using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Result of conditional policy evaluation
/// </summary>
public abstract class ConditionalPolicyResult
{
    /// <summary>
    ///     Whether access is allowed after policy evaluation
    /// </summary>
    public bool IsAllowed { get; set; } = true;

    /// <summary>
    ///     Action taken by the policy
    /// </summary>
    public PolicyAction? Action { get; set; }

    /// <summary>
    ///     Reason for the decision
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    ///     Enforcement message to display to user
    /// </summary>
    public string? EnforcementMessage { get; set; }

    /// <summary>
    ///     Policies that were matched and evaluated
    /// </summary>
    public List<ConditionalPolicyMatch> MatchedPolicies { get; set; } = new List<ConditionalPolicyMatch>();

    /// <summary>
    ///     Additional requirements (e.g., MFA, approval)
    /// </summary>
    public List<string> AdditionalRequirements { get; set; } = new List<string>();

    /// <summary>
    ///     Evaluation duration in milliseconds
    /// </summary>
    public long EvaluationDurationMs { get; set; }
}
