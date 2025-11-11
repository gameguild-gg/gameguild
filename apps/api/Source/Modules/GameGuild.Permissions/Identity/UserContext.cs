using System.Security.Claims;
using GameGuild.Permissions.Domain.Abstractions;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Permissions.Infrastructure.Identity;

/// <summary>
///     Extracts user context from HttpContext claims
/// </summary>
public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public Guid? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                              httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value ?? httpContextAccessor.HttpContext?.User?.FindFirst("user_id")?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }

    public string? Email { get => httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value; }

    public string? Name
    {
        get => httpContextAccessor.HttpContext?.User?.Identity?.Name ?? httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value ?? httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value;
    }

    public bool IsAuthenticated { get => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false; }

    public IDictionary<string, object> Claims
    {
        get
        {
            var claims = new Dictionary<string, object>();
            var user = httpContextAccessor.HttpContext?.User;

            if (user != null)
            {
                foreach (var claim in user.Claims)
                {
                    if (!claims.ContainsKey(claim.Type)) { claims[claim.Type] = claim.Value; }
                }
            }

            return claims;
        }
    }

    public IEnumerable<string> Roles
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;

            if (user == null) return Enumerable.Empty<string>();

            return user.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value).Distinct();
        }
    }

    public bool IsInRole(string role) { return Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)); }
}
