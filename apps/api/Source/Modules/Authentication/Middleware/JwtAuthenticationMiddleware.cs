using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;


namespace GameGuild.Modules.Auth {
  /// <summary>
  /// JWT Authentication middleware
  /// </summary>
  public class JwtAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration) {
    public async Task InvokeAsync(HttpContext context) {
      var token = ExtractTokenFromHeader(context.Request);

      if (!string.IsNullOrEmpty(token)) {
        try {
          var principal = ValidateToken(token);
          context.User = principal;
        }
        catch (Exception) {
          // Token validation failed, continue without setting user
        }
      }

      await next(context);
    }

    private static string? ExtractTokenFromHeader(HttpRequest request) {
      var authHeader = request.Headers.Authorization.FirstOrDefault();

      if (authHeader != null && authHeader.StartsWith("Bearer ")) { return authHeader.Substring("Bearer ".Length).Trim(); }

      return null;
    }

    private ClaimsPrincipal ValidateToken(string token) {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(configuration["Jwt:SecretKey"] ?? "dev-key");

      var validationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5), // Allow 5 minutes clock skew tolerance
      };

      var principal =
        tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

      return principal;
    }
  }
}
