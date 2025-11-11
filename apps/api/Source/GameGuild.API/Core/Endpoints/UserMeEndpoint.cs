using System.Security.Claims;

namespace GameGuild.API.Endpoints;

/// <summary>
///     Minimal endpoints for user-related operations
///     Maps /users/* routes (without /api prefix)
/// </summary>
public static class UserMeEndpoint
{
    /// <summary>
    ///     Maps the /users/me endpoint
    /// </summary>
    public static IEndpointRouteBuilder MapUserMeEndpoint(this IEndpointRouteBuilder app)
    {
        // Map /users/me endpoint - requires authentication
        app.MapGet("/users/me", GetCurrentUser).WithName("GetCurrentUserMe").WithTags("Users").RequireAuthorization().WithOpenApi();

        app.MapPut("/users/me", UpdateCurrentUser).WithName("UpdateCurrentUserMe").WithTags("Users").RequireAuthorization().WithOpenApi();

        return app;
    }

    private static IResult GetCurrentUser(HttpContext context)
    {
        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated != true) { return Results.Unauthorized(); }

        // Get user ID from claims - JWT uses "sub" claim
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                          context.User.FindFirst("sub")?.Value ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

        var emailClaim = context.User.FindFirst(ClaimTypes.Email)?.Value ??
                         context.User.FindFirst("email")?.Value ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;

        var nameClaim = context.User.FindFirst(ClaimTypes.Name)?.Value ?? context.User.FindFirst("username")?.Value ?? context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value;

        if (string.IsNullOrEmpty(userIdClaim)) { return Results.Unauthorized(); }

        // Return basic user info from token
        return Results.Ok(new { id = userIdClaim, email = emailClaim, username = nameClaim, roles = new List<string>() });
    }

    private static IResult UpdateCurrentUser(HttpContext context, UpdateUserRequest request)
    {
        // Check if user is authenticated
        if (context.User?.Identity?.IsAuthenticated != true) { return Results.Unauthorized(); }

        // Get user ID from claims
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim)) { return Results.Unauthorized(); }

        // For now, return NoContent to indicate success
        // In a real implementation, this would update the user in the database
        return Results.NoContent();
    }

    private record UpdateUserRequest(string? FirstName, string? LastName, string? Username);
}
