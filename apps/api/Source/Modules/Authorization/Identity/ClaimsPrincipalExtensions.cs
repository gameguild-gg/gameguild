using System.Security.Claims;


namespace GameGuild.Source.Modules.Authorization.Identity;

/// <summary> Extension methods for ClaimsPrincipal to extract common user information </summary>
public static class ClaimsPrincipalExtensions {
  /// <summary> Extracts the user ID from JWT claims </summary>
  /// <param name="user"> The claims principal </param>
  /// <returns> User ID if found and valid, otherwise null </returns>
  public static Guid? GetUserId(this ClaimsPrincipal user) {
    if (user?.Identity?.IsAuthenticated != true) {
      return null;
    }

    // Prioritize "user_id" claim, then "sub", then NameIdentifier
    var userIdClaim = user.FindFirst("user_id") ?? user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);

    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)) {
      return userId;
    }

    return null;
  }

  /// <summary> Extracts the tenant ID from JWT claims </summary>
  /// <param name="user"> The claims principal </param>
  /// <returns> Tenant ID if found and valid, otherwise null </returns>
  public static Guid? GetTenantId(this ClaimsPrincipal user) {
    if (user?.Identity?.IsAuthenticated != true) {
      return null;
    }

    var tenantIdClaim = user.FindFirst("tenant_id") ?? user.FindFirst("tid") ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid");

    if (tenantIdClaim != null && Guid.TryParse(tenantIdClaim.Value, out var tenantId)) {
      return tenantId;
    }

    return null;
  }

  /// <summary> Gets the user's email from claims </summary>
  /// <param name="user"> The claims principal </param>
  /// <returns> Email if found, otherwise null </returns>
  public static string? GetEmail(this ClaimsPrincipal user) { return user?.FindFirst(ClaimTypes.Email)?.Value ?? user?.FindFirst("email")?.Value; }

  /// <summary> Gets the user's display name from claims </summary>
  /// <param name="user"> The claims principal </param>
  /// <returns> Display name if found, otherwise null </returns>
  public static string? GetDisplayName(this ClaimsPrincipal user) { return user?.FindFirst(ClaimTypes.Name)?.Value ?? user?.FindFirst("name")?.Value ?? user?.FindFirst("preferred_username")?.Value; }
}
