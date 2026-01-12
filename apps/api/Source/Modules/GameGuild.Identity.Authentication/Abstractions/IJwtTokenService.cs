using System.Security.Claims;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for generating and validating JWT tokens
/// </summary>
public interface IJwtTokenService
{
    // Synchronous methods
    string GenerateAccessToken(Guid userId, string email, string[ ] roles);

    string GenerateAccessToken(Guid userId, string email, string[ ] roles, IEnumerable<Claim> additionalClaims);

    string GenerateRefreshToken();

    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

    ClaimsPrincipal ValidateToken(string token);

    // Async methods
    Task<string> GenerateAccessTokenAsync(Guid userId, string email, string[ ] roles, Guid? tenantId, CancellationToken cancellationToken = default);

    Task<string> GenerateRefreshTokenAsync(Guid userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default);
}
