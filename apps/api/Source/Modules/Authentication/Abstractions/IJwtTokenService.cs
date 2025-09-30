using System.Security.Claims;
using GameGuild.Modules.Users;

namespace GameGuild.Modules.Authentication;

public interface IJwtTokenService
{
    string GenerateAccessToken(UserDto user, string[ ] roles);

    string GenerateAccessToken(UserDto user, string[ ] roles, IEnumerable<Claim>? additionalClaims = null);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    ClaimsPrincipal? ValidateToken(string token);
}
