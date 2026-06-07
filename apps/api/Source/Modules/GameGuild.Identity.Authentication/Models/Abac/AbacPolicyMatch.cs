namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents a matched ABAC policy during evaluation
/// </summary>
public abstract class AbacPolicyMatch
{
    /// <summary>
    ///     Policy ID that was matched
    /// </summary>
    public Guid PolicyId { get; set; }

    /// <summary>
    ///     Policy name
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    ///     Decision from this policy
    /// </summary>
    public AbacDecision Decision { get; set; }

    /// <summary>
    ///     Priority of the policy
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Match score (0-1)
    /// </summary>
    public double MatchScore { get; set; }

    /// <summary>
    ///     Reason for this policy decision
    /// </summary>
    public string? Reason { get; set; }
}
