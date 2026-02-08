using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Base controller for authentication controllers that provides common functionality.
///     Inherits from <see cref="BaseApiController"/> for standardized Result-to-ActionResult mapping.
/// </summary>
public abstract class AuthControllerBase : BaseApiController
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

    /// <summary>
    ///     Gets the current user email from the JWT claims
    /// </summary>
    /// <returns>The current user's email address</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when email is not found in token</exception>
    protected string GetCurrentUserEmail()
    {
        var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(emailClaim)) { throw new UnauthorizedAccessException("Email not found in token"); }

        return emailClaim;
    }
}
