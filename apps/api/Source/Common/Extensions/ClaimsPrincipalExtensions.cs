using System.Security.Claims;


namespace GameGuild.Common;

public static class ClaimsPrincipalExtensions {
  public static Guid? GetUserId(this ClaimsPrincipal user) {
    if (user?.Identity?.IsAuthenticated != true) return null;

    var userIdClaim = user.FindFirst("sub") ?? user.FindFirst(ClaimTypes.NameIdentifier);

    if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId)) return userId;

    return null;
  }
}
