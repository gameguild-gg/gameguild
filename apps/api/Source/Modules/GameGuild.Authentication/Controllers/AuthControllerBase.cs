using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Authentication.Controllers;

/// <summary>
///     Base controller for authentication controllers that provides common functionality
/// </summary>
public abstract class AuthControllerBase : ControllerBase
{
    /// <summary>
    ///     Gets the current user ID from the JWT claims
    /// </summary>
    /// <returns>The current user's ID</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when user ID is not found in token</exception>
    protected Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId)) { throw new UnauthorizedAccessException("User ID not found in token"); }

        return userId;
    }
}
