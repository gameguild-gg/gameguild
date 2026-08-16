using GameGuild.Identity.Authorization.Utilities;
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
        var userId = ClaimsExtractor.GetUserIdAsGuid(User);

        if (!userId.HasValue) { throw new UnauthorizedAccessException("User ID not found in token"); }

        return userId.Value;
    }

    /// <summary>
    ///     Gets the current user email from the JWT claims
    /// </summary>
    /// <returns>The current user's email address</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when email is not found in token</exception>
    protected string GetCurrentUserEmail()
    {
        var emailClaim = ClaimsExtractor.GetEmail(User);

        if (string.IsNullOrEmpty(emailClaim)) { throw new UnauthorizedAccessException("Email not found in token"); }

        return emailClaim;
    }
}
