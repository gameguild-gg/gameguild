using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GameGuild.Modules.Auth.Dtos;
using Microsoft.IdentityModel.Tokens;


namespace GameGuild.Modules.Auth.Services {
  public interface IJwtTokenService {
    string GenerateAccessToken(UserDto user, string[] roles);

    string GenerateAccessToken(UserDto user, string[] roles, IEnumerable<Claim>? additionalClaims = null);

    string GenerateRefreshToken();

    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);

    ClaimsPrincipal? ValidateToken(string token);
  }

  public class JwtTokenService(IConfiguration configuration) : IJwtTokenService {
    public string GenerateAccessToken(UserDto user, string[] roles) { return GenerateAccessToken(user, roles, null); }

    public string GenerateAccessToken(UserDto user, string[] roles, IEnumerable<Claim>? additionalClaims = null) {
      var claims = new List<Claim> { new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim("username", user.Username) };

      foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));

      // Add any additional claims (like tenant claims)
      if (additionalClaims != null) claims.AddRange(additionalClaims);

      var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(
          configuration["Jwt:SecretKey"] ??
          "development-fallback-key-that-is-at-least-32-characters-long-for-testing"
        )
      );
      var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

      var expiryMinutes = int.Parse(configuration["Jwt:ExpiryInMinutes"] ?? "60");
      var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

      var token = new JwtSecurityToken(
        issuer: configuration["Jwt:Issuer"],
        audience: configuration["Jwt:Audience"],
        claims: claims,
        expires: expires,
        signingCredentials: creds
      );

      return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken() {
      var randomBytes = new byte[64];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(randomBytes);

      return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token) {
      var tokenValidationParameters = new TokenValidationParameters {
        ValidateAudience = false,
        ValidateIssuer = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes(
            configuration["Jwt:SecretKey"] ??
            "development-fallback-key-that-is-at-least-32-characters-long-for-testing"
          )
        ),
        ValidateLifetime = false, // We don't care about the token's expiration date
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try {
        var principal =
          tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
              SecurityAlgorithms.HmacSha256,
              StringComparison.InvariantCultureIgnoreCase
            ))
          throw new SecurityTokenException("Invalid token");

        return principal;
      }
      catch (Exception ex) {
        // Log the exception for debugging
        Console.WriteLine($"GetPrincipalFromExpiredToken failed: {ex.Message}");
        Console.WriteLine($"Exception type: {ex.GetType().Name}");

        return null;
      }
    }

    public ClaimsPrincipal? ValidateToken(string token) {
      var tokenValidationParameters = new TokenValidationParameters {
        ValidateAudience = true,
        ValidateIssuer = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes(
            configuration["Jwt:SecretKey"] ??
            "development-fallback-key-that-is-at-least-32-characters-long-for-testing"
          )
        ),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew tolerance
      };

      var tokenHandler = new JwtSecurityTokenHandler();

      try {
        var principal =
          tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken ||
            !jwtSecurityToken.Header.Alg.Equals(
              SecurityAlgorithms.HmacSha256,
              StringComparison.InvariantCultureIgnoreCase
            ))
          throw new SecurityTokenException("Invalid token");

        return principal;
      }
      catch { return null; }
    }
  }
}
