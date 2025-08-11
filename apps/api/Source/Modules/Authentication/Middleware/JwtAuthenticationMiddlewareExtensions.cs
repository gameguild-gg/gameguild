namespace GameGuild.Modules.Authentication;

/// <summary>
/// Extension method to register JWT middleware
/// </summary>
public static class JwtAuthenticationMiddlewareExtensions {
  public static IApplicationBuilder UseJwtAuthentication(this IApplicationBuilder builder) { return builder.UseMiddleware<JwtAuthenticationMiddleware>(); }
}
