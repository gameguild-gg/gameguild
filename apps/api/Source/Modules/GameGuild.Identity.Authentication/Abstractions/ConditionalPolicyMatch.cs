using GameGuild.Identity.Authorization;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Represents a matched conditional policy during evaluation
/// </summary>
public abstract class ConditionalPolicyMatch
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
    ///     Action taken by this policy
    /// </summary>
    public PolicyAction Action { get; set; }

    /// <summary>
    ///     Priority of the policy
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    ///     Whether this policy's conditions were met
    /// </summary>
    public bool ConditionsMet { get; set; }

    /// <summary>
    ///     Reason for this policy decision
    /// </summary>
    public string? Reason { get; set; }
}
