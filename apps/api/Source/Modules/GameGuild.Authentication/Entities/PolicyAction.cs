namespace GameGuild.Authentication.Entities;

/// <summary>
///     Actions to take when a conditional policy matches
/// </summary>
public enum PolicyAction
{
    /// <summary>
    ///     Explicitly allow the permission
    /// </summary>
    Allow = 1,

    /// <summary>
    ///     Explicitly deny the permission
    /// </summary>
    Deny = 2,

    /// <summary>
    ///     Require additional MFA verification
    /// </summary>
    Require2FA = 3,

    /// <summary>
    ///     Require approval from designated approver
    /// </summary>
    RequireApproval = 4,

    /// <summary>
    ///     Log but allow (audit-only mode)
    /// </summary>
    LogOnly = 5,

    /// <summary>
    ///     Challenge with CAPTCHA or similar
    /// </summary>
    Challenge = 6
}
