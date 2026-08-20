using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to request a password reset email
/// </summary>
public class RequestPasswordResetCommand : ICommand<PasswordResetRequestResult>
{
    /// <summary>
    ///     Email address to send the reset link to
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    ///     Optional tenant context
    /// </summary>
    public Guid? TenantId { get; init; }

    /// <summary>
    ///     Client's IP address for security logging
    /// </summary>
    public string? IpAddress { get; init; }
}

/// <summary>
///     Result of password reset request
/// </summary>
public class PasswordResetRequestResult
{
    /// <summary>
    ///     Whether the request was processed (always true for security - don't reveal if email exists)
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    ///     Generic message (same regardless of whether email exists for security)
    /// </summary>
    public string Message { get; set; } = "If an account with that email exists, a password reset link has been sent.";

    /// <summary>
    ///     Token expiry time in minutes
    /// </summary>
    public int ExpiresInMinutes { get; set; } = 60;
}

/// <summary>
///     Command to complete a password reset using a token
/// </summary>
public class ResetPasswordCommand : ICommand<PasswordResetResult>
{
    /// <summary>
    ///     Password reset token from the email link
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    ///     New password to set
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    ///     Confirmation of the new password
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>
    ///     Optional tenant context
    /// </summary>
    public Guid? TenantId { get; init; }
}

/// <summary>
///     Result of password reset
/// </summary>
public class PasswordResetResult
{
    /// <summary>
    ///     Whether password was successfully reset
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     User ID whose password was reset
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
///     Command to change password for an authenticated user
/// </summary>
public class ChangePasswordCommand : ICommand<PasswordChangeResult>
{
    /// <summary>
    ///     User ID (from authenticated context)
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    ///     Current password for verification
    /// </summary>
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>
    ///     New password to set
    /// </summary>
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>
    ///     Confirmation of the new password
    /// </summary>
    public string ConfirmPassword { get; init; } = string.Empty;

    /// <summary>
    ///     Whether to revoke all other sessions after password change
    /// </summary>
    public bool RevokeOtherSessions { get; init; } = true;

    /// <summary>
    ///     Session ID of the current access token (extracted from the session_id claim by the controller).
    ///     When set with RevokeOtherSessions, all other sessions are terminated and this one is kept.
    /// </summary>
    public Guid? CurrentSessionId { get; init; }
}

/// <summary>
///     Result of password change
/// </summary>
public class PasswordChangeResult
{
    /// <summary>
    ///     Whether password was successfully changed
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    ///     Status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Number of sessions revoked (if RevokeOtherSessions was true)
    /// </summary>
    public int SessionsRevoked { get; set; }
}
