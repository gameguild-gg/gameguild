using GameGuild.Authentication.DTOs;
using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

/// <summary>
///     Command to handle token refresh
/// </summary>
public class RefreshTokenCommand : IRequest<SignInResponse>
{
    /// <summary>
    ///     The refresh token to use for generating new access/refresh tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    ///     Optional tenant ID to generate tenant-specific claims
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    ///     IP address for audit logging
    /// </summary>
    public string? IpAddress { get; set; }
}
