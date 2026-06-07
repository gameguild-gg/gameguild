namespace GameGuild.Identity.Authentication;

/// <summary>
///     ABAC decision enumeration
/// </summary>
public enum AbacDecision
{
    /// <summary>
    ///     Access is explicitly allowed
    /// </summary>
    Allow = 1,

    /// <summary>
    ///     Access is explicitly denied
    /// </summary>
    Deny = 2,

    /// <summary>
    ///     No applicable policy found
    /// </summary>
    NotApplicable = 3
}
