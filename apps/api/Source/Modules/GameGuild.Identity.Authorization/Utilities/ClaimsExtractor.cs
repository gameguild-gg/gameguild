using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace GameGuild.Identity.Authorization.Utilities;

/// <summary>
///     Utility class for extracting claims from ClaimsPrincipal with consistent fallback logic.
///     Eliminates duplicate claim extraction code across middleware and services.
/// </summary>
/// <remarks>
///     <para>
///         This utility centralizes the claim extraction logic that was previously duplicated
///         across ActorContextMiddleware, TenantMiddleware, TokenRevocationMiddleware, and other components.
///     </para>
///     <para>
///         All extraction methods follow a consistent pattern:
///         - Try primary claim type first (e.g., JWT standard claims)
///         - Fall back to alternate claim types (.NET ClaimTypes)
///         - Return null for missing claims (never throw)
///         - Use case-insensitive comparison where appropriate
///     </para>
/// </remarks>
public static class ClaimsExtractor
{
    /// <summary>
    ///     Extracts the user ID (subject) from claims.
    ///     Tries: sub -> NameIdentifier -> UserId
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>User ID string, or null if not found</returns>
    public static string? GetUserId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimNames.Subject)?.Value
            ?? user.FindFirst(ClaimNames.NameIdentifier)?.Value
            ?? user.FindFirst(ClaimNames.UserId)?.Value;
    }

    /// <summary>
    ///     Extracts the user ID and parses it as a Guid.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>User ID as Guid, or null if not found or invalid</returns>
    public static Guid? GetUserIdAsGuid(ClaimsPrincipal user)
    {
        var userIdStr = GetUserId(user);
        return string.IsNullOrWhiteSpace(userIdStr) 
            ? null 
            : Guid.TryParse(userIdStr, out var userId) ? userId : null;
    }

    /// <summary>
    ///     Extracts the JWT ID (jti) claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>JWT ID, or null if not found</returns>
    public static string? GetJti(ClaimsPrincipal user)
    {
        return user.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
    }

    /// <summary>
    ///     Extracts the issued-at timestamp (iat) claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Issued-at timestamp as Unix seconds, or null if not found</returns>
    public static long? GetIssuedAt(ClaimsPrincipal user)
    {
        var iatStr = user.FindFirst(JwtRegisteredClaimNames.Iat)?.Value;
        return string.IsNullOrWhiteSpace(iatStr)
            ? null
            : long.TryParse(iatStr, out var iat) ? iat : null;
    }

    /// <summary>
    ///     Extracts the issued-at timestamp and converts to DateTime.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Issued-at as UTC DateTime, or null if not found</returns>
    public static DateTime? GetIssuedAtDateTime(ClaimsPrincipal user)
    {
        var iat = GetIssuedAt(user);
        return iat.HasValue 
            ? DateTimeOffset.FromUnixTimeSeconds(iat.Value).UtcDateTime 
            : null;
    }

    /// <summary>
    ///     Extracts the email claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Email address, or null if not found</returns>
    public static string? GetEmail(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimNames.Email)?.Value
            ?? user.FindFirst(ClaimTypes.Email)?.Value;
    }

    /// <summary>
    ///     Extracts the name claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>User name, or null if not found</returns>
    public static string? GetName(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value
            ?? user.FindFirst("name")?.Value;
    }

    /// <summary>
    ///     Extracts all role claims.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Set of role names (case-insensitive)</returns>
    public static HashSet<string> GetRoles(ClaimsPrincipal user)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims)
        {
            if (claim.Type == ClaimTypes.Role || 
                claim.Type == ClaimNames.Role || 
                claim.Type == "roles")
            {
                roles.Add(claim.Value);
            }
        }

        return roles;
    }

    /// <summary>
    ///     Extracts the tenant ID claim.
    ///     Tries: TenantId -> tenant_id
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Tenant ID string, or null if not found</returns>
    public static string? GetTenantId(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimNames.TenantId)?.Value
            ?? user.FindFirst(ClaimNames.TenantIdAlt)?.Value;
    }

    /// <summary>
    ///     Extracts the tenant ID and parses it as a Guid.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Tenant ID as Guid, or null if not found or invalid</returns>
    public static Guid? GetTenantIdAsGuid(ClaimsPrincipal user)
    {
        var tenantIdStr = GetTenantId(user);
        return string.IsNullOrWhiteSpace(tenantIdStr)
            ? null
            : Guid.TryParse(tenantIdStr, out var tenantId) ? tenantId : null;
    }

    /// <summary>
    ///     Extracts the grant_type claim (used for client credentials detection).
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Grant type, or null if not found</returns>
    public static string? GetGrantType(ClaimsPrincipal user)
    {
        return user.FindFirst("grant_type")?.Value;
    }

    /// <summary>
    ///     Extracts the actor_type claim (for service/system/webhook actors).
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Actor type, or null if not found</returns>
    public static string? GetActorType(ClaimsPrincipal user)
    {
        return user.FindFirst("actor_type")?.Value;
    }

    /// <summary>
    ///     Extracts all permission claims.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Set of permission names (case-insensitive)</returns>
    public static HashSet<string> GetPermissions(ClaimsPrincipal user)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in user.Claims)
        {
            if (claim.Type == "permission" || claim.Type == "permissions")
            {
                permissions.Add(claim.Value);
            }
        }

        return permissions;
    }

    /// <summary>
    ///     Extracts the MFA verified claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if MFA is verified, false otherwise</returns>
    public static bool IsMfaVerified(ClaimsPrincipal user)
    {
        var mfaVerified = user.FindFirst(ClaimNames.MfaVerified)?.Value;
        return !string.IsNullOrWhiteSpace(mfaVerified) 
            && bool.TryParse(mfaVerified, out var verified) 
            && verified;
    }

    /// <summary>
    ///     Extracts the email_verified claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if email is verified, false otherwise</returns>
    public static bool IsEmailVerified(ClaimsPrincipal user)
    {
        var emailVerified = user.FindFirst(ClaimNames.EmailVerified)?.Value;
        return !string.IsNullOrWhiteSpace(emailVerified) 
            && bool.TryParse(emailVerified, out var verified) 
            && verified;
    }

    /// <summary>
    ///     Extracts the Authentication Method Reference (amr) claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>AMR value, or null if not found</returns>
    public static string? GetAmr(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimNames.Amr)?.Value;
    }

    /// <summary>
    ///     Extracts the token version claim.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>Token version string, or null if not found</returns>
    public static string? GetTokenVersion(ClaimsPrincipal user)
    {
        return user.FindFirst("token_version")?.Value;
    }

    /// <summary>
    ///     Extracts a custom claim by type.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <param name="claimType">The claim type to extract</param>
    /// <returns>Claim value, or null if not found</returns>
    public static string? GetClaim(ClaimsPrincipal user, string claimType)
    {
        return user.FindFirst(claimType)?.Value;
    }

    /// <summary>
    ///     Checks if the user is authenticated.
    /// </summary>
    /// <param name="user">The claims principal</param>
    /// <returns>True if authenticated, false otherwise</returns>
    public static bool IsAuthenticated(ClaimsPrincipal user)
    {
        return user.Identity?.IsAuthenticated ?? false;
    }
}
