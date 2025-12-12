namespace GameGuild.Authentication.Entities;

/// <summary>
///     ABAC policy effect enumeration
/// </summary>
public enum AbacPolicyEffect
{
    /// <summary>
    ///     Policy allows access when conditions are met
    /// </summary>
    Allow = 1,

    /// <summary>
    ///     Policy denies access when conditions are met
    /// </summary>
    Deny = 2
}
