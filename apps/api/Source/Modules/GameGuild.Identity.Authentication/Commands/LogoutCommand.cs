using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Command to logout a user and immediately revoke all their tokens.
///     This enables immediate logout without waiting for token expiry.
/// </summary>
public sealed class LogoutCommand : IRequest<LogoutResponse>
{
    /// <summary>
    ///     The user ID to logout
    /// </summary>
    public required Guid UserId { get; init; }

    /// <summary>
    ///     Optional: The current access token's JTI to revoke specifically
    /// </summary>
    public string? CurrentTokenJti { get; init; }

    /// <summary>
    ///     Optional: The current access token's expiry time
    /// </summary>
    public DateTime? CurrentTokenExpiresAt { get; init; }

    /// <summary>
    ///     Whether to revoke all user sessions (logout everywhere)
    /// </summary>
    public bool LogoutEverywhere { get; init; }

    /// <summary>
    ///     Reason for logout (for audit)
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    ///     IP address for audit logging
    /// </summary>
    public string? IpAddress { get; init; }
}

/// <summary>
///     Response from logout command
/// </summary>
public sealed class LogoutResponse
{
    /// <summary>
    ///     Whether the logout was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    ///     Message describing the result
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    ///     Number of sessions invalidated
    /// </summary>
    public int SessionsInvalidated { get; init; }
}
