using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameGuild.Identity.Authorization.Utilities;
using GameGuild.Identity.Users;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Middleware that validates JWT tokens against the revocation list and token version.
///     Rejects requests with revoked tokens or mismatched token versions, enabling immediate logout functionality.
/// </summary>
/// <remarks>
///     <para>
///         <b>Execution Order:</b> This middleware should run AFTER authentication middleware
///         but BEFORE authorization middleware. It requires a valid ClaimsPrincipal.
///     </para>
///     <para>
///         <b>Performance:</b> Uses async operations to check revocation status and token version.
///         For high-throughput scenarios, consider caching token version with short TTL.
///     </para>
///     <para>
///         <b>Token Version:</b> Compares the token's version claim against the user's current
///         token version in the database. If the token version is lower, the token is rejected.
///         This enables immediate invalidation of all tokens when a user changes their password
///         or initiates "logout all sessions".
///     </para>
/// </remarks>
public sealed class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRevocationMiddleware> _logger;

    public TokenRevocationMiddleware(RequestDelegate next, ILogger<TokenRevocationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenRevocationService revocationService, IUserRepository userRepository)
    {
        // Skip if not authenticated
        if (!ClaimsExtractor.IsAuthenticated(context.User))
        {
            await _next(context);
            return;
        }

        // Extract JTI from claims
        var jti = ClaimsExtractor.GetJti(context.User);
        
        if (!string.IsNullOrEmpty(jti))
        {
            // Check if individual token is revoked
            if (await revocationService.IsRevokedAsync(jti, context.RequestAborted))
            {
                _logger.LogWarning("Rejected request with revoked token: JTI={Jti}", jti);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Token has been revoked" });
                return;
            }
        }

        // Extract user ID and token issued time for user-level revocation check
        var userId = ClaimsExtractor.GetUserIdAsGuid(context.User);
        var tokenIssuedAt = ClaimsExtractor.GetIssuedAtDateTime(context.User);

        if (userId.HasValue && tokenIssuedAt.HasValue)
        {
            // Check if all user tokens were revoked after this token was issued
            if (await revocationService.IsUserTokenRevokedAsync(userId.Value, tokenIssuedAt.Value, context.RequestAborted))
            {
                _logger.LogWarning(
                    "Rejected request with user-revoked token: UserId={UserId}, IssuedAt={IssuedAt}",
                    userId, tokenIssuedAt);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "All user sessions have been revoked" });
                return;
            }
        }

        // Token version validation: compare JWT's token_version against user's current TokenVersion
        if (userId.HasValue)
        {
            var tokenVersionClaim = ClaimsExtractor.GetTokenVersion(context.User);
            if (!string.IsNullOrEmpty(tokenVersionClaim) && int.TryParse(tokenVersionClaim, out var tokenVersion))
            {
                var currentVersion = await userRepository.GetTokenVersionAsync(userId.Value, context.RequestAborted);
                
                // If user exists and token version is outdated, reject the token
                if (currentVersion.HasValue && tokenVersion < currentVersion.Value)
                {
                    _logger.LogWarning(
                        "Rejected request with outdated token version: UserId={UserId}, TokenVersion={TokenVersion}, CurrentVersion={CurrentVersion}",
                        userId, tokenVersion, currentVersion);
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new 
                    { 
                        error = "token_version_mismatch",
                        message = "Your session has been invalidated. Please sign in again."
                    });
                    return;
                }
            }
        }

        await _next(context);
    }
}
