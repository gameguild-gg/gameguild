using System.Security.Claims;


namespace GameGuild.Common;

/// <summary>
/// Implementation of user context from HTTP context
/// </summary>
public class UsersContext : IUserContext {
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly ClaimsPrincipal? _user;

  public UsersContext(IHttpContextAccessor httpContextAccessor) {
    _httpContextAccessor = httpContextAccessor;
    _user = _httpContextAccessor.HttpContext?.User;
  }

  public Guid? UserId {
    get {
      var userIdClaim = _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? _user?.FindFirst("sub")?.Value ?? _user?.FindFirst("user_id")?.Value;

      return Guid.TryParse(userIdClaim, out var id) ? id : null;
    }
  }

  public string? Email => _user?.FindFirst(ClaimTypes.Email)?.Value ?? _user?.FindFirst("email")?.Value;

  public string? Name => _user?.FindFirst(ClaimTypes.Name)?.Value ?? _user?.FindFirst("name")?.Value ?? _user?.FindFirst("preferred_username")?.Value;

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

  public IEnumerable<string> Roles => _user?.FindAll(ClaimTypes.Role)?.Select(c => c.Value) ?? Enumerable.Empty<string>();
}

/// <summary>
/// Implementation of tenant context from HTTP context
/// </summary>
public class TenantContext : ITenantContext {
  private readonly IHttpContextAccessor _httpContextAccessor;
  private readonly ClaimsPrincipal? _user;
  private readonly IHttpContextAccessor _accessor;

  public TenantContext(IHttpContextAccessor httpContextAccessor) {
    _httpContextAccessor = httpContextAccessor;
    _accessor = httpContextAccessor;
    _user = _httpContextAccessor.HttpContext?.User;
  }

  public Guid? TenantId {
    get {
      // Try to get tenant from claims first
      var tenantIdClaim = _user?.FindFirst("tenant_id")?.Value ?? _user?.FindFirst("tid")?.Value;

      if (Guid.TryParse(tenantIdClaim, out var claimTenantId)) return claimTenantId;

      // Try to get from headers
      var headerTenantId = _accessor.HttpContext?.Request.Headers["X-Tenant-ID"].FirstOrDefault();

      if (Guid.TryParse(headerTenantId, out var headerTid)) return headerTid;

      // Try to get from query string
      var queryTenantId = _accessor.HttpContext?.Request.Query["tenantId"].FirstOrDefault();

      if (Guid.TryParse(queryTenantId, out var queryTid)) return queryTid;

      return null;
    }
  }

  public string? TenantName => _user?.FindFirst("tenant_name")?.Value ?? _accessor.HttpContext?.Request.Headers["X-Tenant-Name"].FirstOrDefault();

  public IDictionary<string, object> Settings {
    get {
      var settings = new Dictionary<string, object>();

      // Extract tenant-specific claims
      if (_user != null) {
        foreach (var claim in _user.Claims.Where(c => c.Type.StartsWith("tenant_"))) { settings[claim.Type] = claim.Value; }
      }

      return settings;
    }
  }

  public bool IsActive => _user?.FindFirst("tenant_active")?.Value == "true";

  public string? SubscriptionPlan => _user?.FindFirst("subscription_plan")?.Value ?? _user?.FindFirst("tenant_plan")?.Value;
}
