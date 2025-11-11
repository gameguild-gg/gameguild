namespace GameGuild.Authentication.Entities;

/// <summary>
///     Access review decision
/// </summary>
public enum AccessReviewDecision
{
    /// <summary>
    ///     Approve continued access
    /// </summary>
    Approve = 1,

    /// <summary>
    ///     Revoke access
    /// </summary>
    Revoke = 2,

    /// <summary>
    ///     Modify permissions (reduce access level)
    /// </summary>
    Modify = 3,

    /// <summary>
    ///     Escalate for further review
    /// </summary>
    Escalate = 4
}
