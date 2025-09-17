using System.Security.Claims;


namespace GameGuild.Core.Infrastructure.Identity;

/// <summary> Implementation of tenant context from HTTP context Provides access to current tenant information from claims, headers, and query parameters </summary>
public class TenantContext : Domain.Identity.ITenantContext {
  private readonly IHttpContextAccessor _httpContextAccessor;

  private readonly ILogger<TenantContext> _logger;

  private readonly ClaimsPrincipal? _user;

  public TenantContext(IHttpContextAccessor httpContextAccessor, ILogger<TenantContext> logger) {
    _httpContextAccessor = httpContextAccessor;
    _user = _httpContextAccessor.HttpContext?.User;
    _logger = logger;
  }

  public Guid? TenantId {
    get {
      // Try to get tenant from claims first (most common for JWT tokens)
      var tenantIdClaim = _user?.FindFirst("tenant_id")?.Value ?? _user?.FindFirst("tid")?.Value ?? _user?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

      if (Guid.TryParse(tenantIdClaim, out var claimTenantId)) {
        _logger.LogDebug("Found tenant ID from claims: {TenantId}", claimTenantId);

        return claimTenantId;
      }

      // Try to get from headers
      var headerTenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-ID"].FirstOrDefault();

      if (Guid.TryParse(headerTenantId, out var headerTid)) {
        _logger.LogDebug("Found tenant ID from header: {TenantId}", headerTid);

        return headerTid;
      }

      // Try to get from query string
      var queryTenantId = _httpContextAccessor.HttpContext?.Request.Query["tenantId"].FirstOrDefault();

      if (Guid.TryParse(queryTenantId, out var queryTid)) {
        _logger.LogDebug("Found tenant ID from query: {TenantId}", queryTid);

        return queryTid;
      }

      _logger.LogInformation("No tenant ID found in claims, headers, or query. Available claims: {Claims}", string.Join(", ", _user?.Claims?.Select(c => $"{c.Type}={c.Value}") ?? []));

      return null;
    }
  }

  public string? TenantName { get => _user?.FindFirst("tenant_name")?.Value ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Name"].FirstOrDefault(); }

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

  public bool IsActive { get => _user?.FindFirst("tenant_active")?.Value == "true"; }

  public string? SubscriptionPlan { get => _user?.FindFirst("subscription_plan")?.Value ?? _user?.FindFirst("tenant_plan")?.Value; }
}
