using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to handle token revocation
/// </summary>
public class RevokeTokenCommand : IRequest<Unit>
{
    /// <summary>
    ///     The refresh token to revoke
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     IP address for audit logging
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    ///     User ID initiating the revocation
    /// </summary>
    public Guid? UserId { get; set; }
}
