using GameGuild.Modules.Tenants;


namespace GameGuild.Tests.Fixtures;

public class MockTenantService : ITenantService {
  private readonly Dictionary<Guid, Tenant> _tenants = new();
  private readonly Dictionary<(Guid userId, Guid tenantId), TenantPermission> _permissions = new();
  private Guid? _defaultTenantId;

  public MockTenantService() {
    // Create a default test tenant
    var testTenant = new Tenant { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "Test Tenant", Slug = "test-tenant", IsActive = true };
    _tenants.Add(testTenant.Id, testTenant);
    _defaultTenantId = testTenant.Id; // Set as default
  }

  public Task<IEnumerable<Tenant>> GetAllTenantsAsync() { return Task.FromResult<IEnumerable<Tenant>>(_tenants.Values); }

  public Task<Tenant?> GetTenantByIdAsync(Guid id) {
    _tenants.TryGetValue(id, out var tenant);

    return Task.FromResult(tenant);
  }

  public Task<Tenant?> GetTenantByNameAsync(string name) {
    var tenant = _tenants.Values.FirstOrDefault(t => t.Name == name);

    return Task.FromResult(tenant);
  }

  public Task<Tenant> CreateTenantAsync(Tenant tenant) {
    tenant.Id = Guid.NewGuid();
    _tenants.Add(tenant.Id, tenant);

    return Task.FromResult(tenant);
  }

  public Task<Tenant> UpdateTenantAsync(Tenant tenant) {
    _tenants[tenant.Id] = tenant;

    return Task.FromResult(tenant);
  }

  public Task<bool> SoftDeleteTenantAsync(Guid id) {
    if (_tenants.TryGetValue(id, out var tenant)) {
      tenant.SoftDelete();

      return Task.FromResult(true);
    }

    return Task.FromResult(false);
  }

  public Task<bool> RestoreTenantAsync(Guid id) {
    if (_tenants.TryGetValue(id, out var tenant)) {
      tenant.Restore();

      return Task.FromResult(true);
    }

    return Task.FromResult(false);
  }

  public Task<bool> HardDeleteTenantAsync(Guid id) { return Task.FromResult(_tenants.Remove(id)); }

  public Task<bool> ActivateTenantAsync(Guid id) {
    if (_tenants.TryGetValue(id, out var tenant)) {
      tenant.IsActive = true;

      return Task.FromResult(true);
    }

    return Task.FromResult(false);
  }

  public Task<bool> DeactivateTenantAsync(Guid id) {
    if (_tenants.TryGetValue(id, out var tenant)) {
      tenant.IsActive = false;

      return Task.FromResult(true);
    }

    return Task.FromResult(false);
  }

  public Task<IEnumerable<Tenant>> GetDeletedTenantsAsync() {
    var deletedTenants = _tenants.Values.Where(t => t.IsDeleted);

    return Task.FromResult<IEnumerable<Tenant>>(deletedTenants);
  }

  public Task<TenantPermission> AddUserToTenantAsync(Guid userId, Guid tenantId) {
    var permission = new TenantPermission { Id = Guid.NewGuid(), UserId = userId, TenantId = tenantId };
    _permissions[(userId, tenantId)] = permission;

    return Task.FromResult(permission);
  }

  public Task<bool> RemoveUserFromTenantAsync(Guid userId, Guid tenantId) { return Task.FromResult(_permissions.Remove((userId, tenantId))); }

  public Task<IEnumerable<TenantPermission>> GetUsersInTenantAsync(Guid tenantId) {
    var permissions = _permissions.Values.Where(p => p.TenantId == tenantId);

    return Task.FromResult<IEnumerable<TenantPermission>>(permissions);
  }

  public Task<IEnumerable<TenantPermission>> GetTenantsForUserAsync(Guid userId) {
    var permissions = _permissions.Values.Where(p => p.UserId == userId);

    return Task.FromResult<IEnumerable<TenantPermission>>(permissions);
  }

  // === DEFAULT TENANT FUNCTIONALITY ===

  public Task<Tenant> GetOrCreateDefaultTenantAsync() {
    if (_defaultTenantId.HasValue && _tenants.TryGetValue(_defaultTenantId.Value, out var defaultTenant)) {
      return Task.FromResult(defaultTenant);
    }

    // Create a default tenant if none exists
    var tenant = new Tenant {
      Id = Guid.NewGuid(),
      Name = "Default Tenant",
      Slug = "default",
      IsActive = true
    };

    _tenants.Add(tenant.Id, tenant);
    _defaultTenantId = tenant.Id;

    return Task.FromResult(tenant);
  }

  public Task<Tenant?> GetDefaultTenantAsync() {
    if (_defaultTenantId.HasValue && _tenants.TryGetValue(_defaultTenantId.Value, out var defaultTenant)) {
      return Task.FromResult<Tenant?>(defaultTenant);
    }

    return Task.FromResult<Tenant?>(null);
  }

  public Task<Tenant> SetDefaultTenantAsync(Guid tenantId) {
    if (!_tenants.TryGetValue(tenantId, out var tenant)) {
      throw new ArgumentException("Tenant not found", nameof(tenantId));
    }

    _defaultTenantId = tenantId;
    return Task.FromResult(tenant);
  }

  public Task<bool> IsDefaultTenantAsync(Guid tenantId) {
    return Task.FromResult(_defaultTenantId == tenantId);
  }

  public async Task<Tenant> GetEffectiveTenantAsync(Guid? tenantId) {
    if (tenantId.HasValue && _tenants.TryGetValue(tenantId.Value, out var tenant)) {
      return tenant;
    }

    return await GetOrCreateDefaultTenantAsync();
  }
}
