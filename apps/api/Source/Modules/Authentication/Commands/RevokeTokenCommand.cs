using MediatR;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to handle token revocation using CQRS pattern
/// </summary>
public class RevokeTokenCommand : IRequest<Unit>
{
    /// <summary>
    /// The refresh token to revoke
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// IP address for audit logging
    /// </summary>
    public string? IpAddress { get; set; }
}
