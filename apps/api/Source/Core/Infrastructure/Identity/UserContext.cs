using System.Security.Claims;
using GameGuild.Core.Domain.Identity;


namespace GameGuild.Core.Infrastructure.Identity;

/// <summary>
/// Implementation of user context from HTTP context
/// Provides access to current user information from claims
/// </summary>
public class UserContext : GameGuild.Core.Domain.Identity.IUserContext {
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly ClaimsPrincipal? _user;
  private readonly ILogger<UserContext> _logger;

  public UserContext(IHttpContextAccessor httpContextAccessor, ILogger<UserContext> logger) {
    _httpContextAccessor = httpContextAccessor;
    _user = _httpContextAccessor.HttpContext?.User;
    _logger = logger;
  }

  public Guid? UserId {
    get {
      var userIdClaim = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                        _user?.FindFirst("sub")?.Value ??
                        _user?.FindFirst("user_id")?.Value ??
                        _user?.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

      if (Guid.TryParse(userIdClaim, out var id)) {
        _logger.LogDebug("Found user ID: {UserId} from claim: {Claim}", id, userIdClaim);
        return id;
      }

      _logger.LogWarning("Could not extract user ID from token claims. Available claims: {Claims}",
        string.Join(", ", _user?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? Array.Empty<string>()));

      return null;
    }
  }

  public string? Email => _user?.FindFirst(ClaimTypes.Email)?.Value ?? _user?.FindFirst("email")?.Value;

  public string? Name => _user?.FindFirst(ClaimTypes.Name)?.Value ??
                         _user?.FindFirst("name")?.Value ??
                         _user?.FindFirst("preferred_username")?.Value;

  public IDictionary<string, object> Claims {
    get {
      if (_user == null) return new Dictionary<string, object>();

      return _user.Claims.ToDictionary(
        c => c.Type,
        c => (object)c.Value
      );
    }
  }

  public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

  public bool IsInRole(string role) => _user?.IsInRole(role) ?? false;

  public IEnumerable<string> Roles => _user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ??
                                      Enumerable.Empty<string>();
}
