using System.Security.Claims;
using GameGuild.Modules.Authentication;
using GameGuild.Modules.Tenants;

namespace GameGuild.API.Tests.Helpers {
  /// <summary>
  /// Mock implementation of ITenantContextService for testing purposes
  /// </summary>
  public class MockTenantContextService : ITenantContextService {
    private readonly Dictionary<Guid, Tenant> _tenants = new();

    private readonly Dictionary<(Guid userId, Guid tenantId), TenantPermission> _permissions = new();

    /// <summary>
    /// Initialize the mock service with test data
    /// </summary>
    public MockTenantContextService() {
      // Create default test tenant
      var testTenant = new Tenant { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Test Tenant", Slug = "test-tenant", IsActive = true };

      _tenants.Add(testTenant.Id, testTenant);
    }

    /// <summary>
    /// Add a test tenant to the mock service
    /// </summary>
    public void AddTenant(Tenant tenant) { _tenants[tenant.Id] = tenant; }

    /// <summary>
    /// Add a test tenant permission to the mock service
    /// </summary>
    public void AddTenantPermission(TenantPermission permission) {
      if (permission is { UserId: not null, TenantId: not null }) _permissions[(permission.UserId.Value, permission.TenantId.Value)] = permission;
    }

    /// <summary>
    /// Get the current tenant ID from the request or claims
    /// </summary>
    public Task<Guid?> GetCurrentTenantIdAsync(ClaimsPrincipal? user = null, string? tenantHeader = null) {
      // Check header first (highest priority)
      if (!string.IsNullOrEmpty(tenantHeader) && Guid.TryParse(tenantHeader, out var tenantHeaderId))
        // Verify tenant exists
        if (_tenants.ContainsKey(tenantHeaderId) && _tenants[tenantHeaderId].IsActive)
          return Task.FromResult<Guid?>(tenantHeaderId);

      // Check user claims
      if (user?.Identity?.IsAuthenticated == true) {
        var tenantClaim = user.FindFirst(JwtClaimTypes.TenantId);

        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantClaimId))
          // Verify tenant exists
          if (_tenants.ContainsKey(tenantClaimId) && _tenants[tenantClaimId].IsActive)
            return Task.FromResult<Guid?>(tenantClaimId);
      }

      // Return default test tenant if available
      if (_tenants.Any(t => t.Value.IsActive)) return Task.FromResult<Guid?>(_tenants.First(t => t.Value.IsActive).Key);

      return Task.FromResult<Guid?>(null);
    }

    /// <summary>
    /// Get the current tenant entity 
    /// </summary>
    public async Task<Tenant?> GetCurrentTenantAsync(ClaimsPrincipal? user = null, string? tenantHeader = null) {
      var tenantId = await GetCurrentTenantIdAsync(user, tenantHeader);

      if (!tenantId.HasValue) return null;

      return _tenants.TryGetValue(tenantId.Value, out var tenant) && tenant.IsActive ? tenant : null;
    }

    /// <summary>
    /// Get permission data for user in the specified tenant
    /// </summary>
    public Task<TenantPermission?> GetTenantPermissionAsync(Guid userId, Guid tenantId) {
      if (_permissions.TryGetValue((userId, tenantId), out var permission) && permission.IsValid) return Task.FromResult<TenantPermission?>(permission);

      // For testing, if a permission doesn't exist but the user and tenant do,
      // create a default permission with access
      if (!_permissions.ContainsKey((userId, tenantId)) && _tenants.ContainsKey(tenantId)) {
        var newPermission = new TenantPermission {
          Id = Guid.NewGuid(),
          UserId = userId,
          TenantId = tenantId,
          // Set some default permissions
          PermissionFlags1 = 0x000000000000000F // First 4 permissions enabled
        };

        _permissions[(userId, tenantId)] = newPermission;

        return Task.FromResult<TenantPermission?>(newPermission);
      }

      return Task.FromResult<TenantPermission?>(null);
    }

    /// <summary>
    /// Check if user has permission to access the specified tenant
    /// </summary>
    public async Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, Guid tenantId) {
      if (user.Identity?.IsAuthenticated != true ||
          string.IsNullOrEmpty(user.FindFirst(ClaimTypes.NameIdentifier)?.Value))
        return false;

      var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
      var permission = await GetTenantPermissionAsync(userId, tenantId);

      return permission != null;
    }
  }
}
