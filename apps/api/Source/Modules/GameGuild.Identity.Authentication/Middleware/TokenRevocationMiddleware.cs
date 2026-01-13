using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameGuild.Identity.Authorization.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GameGuild.Identity.Authentication;

/// <summary>
///     Middleware that validates JWT tokens against the revocation list.
///     Rejects requests with revoked tokens, enabling immediate logout functionality.
/// </summary>
/// <remarks>
///     <para>
///         <b>Execution Order:</b> This middleware should run AFTER authentication middleware
///         but BEFORE authorization middleware. It requires a valid ClaimsPrincipal.
///     </para>
///     <para>
///         <b>Performance:</b> Uses async operations to check revocation status.
///         For high-throughput scenarios, consider caching revocation status with short TTL.
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

    public async Task InvokeAsync(HttpContext context, ITokenRevocationService revocationService)
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

        await _next(context);
    }
}
