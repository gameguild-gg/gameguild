using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace GameGuild.Identity.Authorization;

/// <summary>
///     Implementation of IUserContext that extracts user information from HttpContext claims
/// </summary>
public class UserContext : IUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _userId;
    private string? _email;
    private string? _name;
    private bool? _isAuthenticated;
    private IDictionary<string, object>? _claims;
    private IEnumerable<string>? _roles;

    public UserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public Guid? UserId
    {
        get
        {
            if (_userId.HasValue)
                return _userId;

            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("sub")?.Value
                ?? User?.FindFirst("userId")?.Value;

            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                _userId = userId;
            }

            return _userId;
        }
    }

    /// <inheritdoc />
    public string? Email
    {
        get
        {
            if (_email != null)
                return _email;

            _email = User?.FindFirst(ClaimTypes.Email)?.Value
                ?? User?.FindFirst("email")?.Value;

            return _email;
        }
    }

    /// <inheritdoc />
    public string? Name
    {
        get
        {
            if (_name != null)
                return _name;

            _name = User?.FindFirst(ClaimTypes.Name)?.Value
                ?? User?.FindFirst("name")?.Value
                ?? User?.FindFirst("preferred_username")?.Value;

            return _name;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated
    {
        get
        {
            if (_isAuthenticated.HasValue)
                return _isAuthenticated.Value;

            _isAuthenticated = User?.Identity?.IsAuthenticated ?? false;

            return _isAuthenticated.Value;
        }
    }

    /// <inheritdoc />
    public IDictionary<string, object> Claims
    {
        get
        {
            if (_claims != null)
                return _claims;

            _claims = new Dictionary<string, object>();

            if (User != null)
            {
                foreach (var claim in User.Claims)
                {
                    if (!_claims.ContainsKey(claim.Type))
                    {
                        _claims[claim.Type] = claim.Value;
                    }
                }
            }

            return _claims;
        }
    }

    /// <inheritdoc />
    public IEnumerable<string> Roles
    {
        get
        {
            if (_roles != null)
                return _roles;

            _roles = User?.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                .Select(c => c.Value)
                .ToList() ?? Enumerable.Empty<string>();

            return _roles;
        }
    }

    /// <inheritdoc />
    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
