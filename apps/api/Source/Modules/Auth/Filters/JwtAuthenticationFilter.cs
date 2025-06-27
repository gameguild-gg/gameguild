using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameGuild.Modules.Auth.Attributes;


namespace GameGuild.Modules.Auth.Filters {
  /// <summary>
  /// JWT Authentication filter that validates tokens and sets user context
  /// </summary>
  public class JwtAuthenticationFilter : IAuthorizationFilter {
    private readonly IConfiguration _configuration;

    public JwtAuthenticationFilter(IConfiguration configuration) { _configuration = configuration; }

    public virtual void OnAuthorization(AuthorizationFilterContext context) {
      // Check if the action/controller is marked as public
      var publicAttribute =
        context.ActionDescriptor.EndpointMetadata.OfType<PublicAttribute>().FirstOrDefault();

      if (publicAttribute?.IsPublic == true) {
        return; // Skip authentication for public endpoints
      }

      // Check for AllowAnonymous attribute
      if (context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any()) { return; }

      var token = ExtractTokenFromHeader(context.HttpContext.Request);

      if (string.IsNullOrEmpty(token)) {
        context.Result = new UnauthorizedResult();

        return;
      }

      try {
        var principal = ValidateToken(token);
        context.HttpContext.User = principal;
      }
      catch (Exception) { context.Result = new UnauthorizedResult(); }
    }

    private string? ExtractTokenFromHeader(HttpRequest request) {
      var authHeader = request.Headers["Authorization"].FirstOrDefault();

      if (authHeader != null && authHeader.StartsWith("Bearer ")) { return authHeader.Substring("Bearer ".Length).Trim(); }

      return null;
    }

    private ClaimsPrincipal ValidateToken(string token) {
      var tokenHandler = new JwtSecurityTokenHandler();
      var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] ?? "dev-key");

      var validationParameters = new TokenValidationParameters {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = _configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = _configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(5) // Allow 5 minutes clock skew tolerance
      };

      var principal =
        tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

      return principal;
    }
  }
}
