using MediatR;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Command to handle token refresh using CQRS pattern
/// </summary>
public class RefreshTokenCommand : IRequest<SignInResponseDto>
{
    /// <summary>
    /// The refresh token to use for generating new access/refresh tokens
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional tenant ID to generate tenant-specific claims
    /// </summary>
    public Guid? TenantId { get; set; }
}
