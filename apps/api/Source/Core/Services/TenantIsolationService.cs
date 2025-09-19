using GameGuild.Modules.Tenants;


namespace GameGuild.Source.Core.Services;

/// <summary>
/// Service for managing tenant isolation 
/// Provides automatic tenant-scoped data filtering for all ITenantable entities
/// </summary>
public interface ITenantIsolationService {
  /// <summary>
  /// Apply tenant filtering to a queryable
  /// </summary>
  /// <typeparam name="T">Entity type implementing ITenantable</typeparam>
  /// <param name="query">Source queryable</param>
  /// <param name="tenantId">Tenant ID to filter by</param>
  /// <param name="includeGlobal">Whether to include global entities (Tenant = null)</param>
  /// <returns>Filtered queryable</returns>
  IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query, Guid? tenantId, bool includeGlobal = true) where T : class, ITenantable;

  /// <summary>
  /// Temporarily disable tenant isolation for administrative operations
  /// </summary>
  /// <returns>Disposable that re-enables filtering when disposed</returns>
  IDisposable DisableTenantIsolation();

  /// <summary>
  /// Get the current tenant ID from context
  /// </summary>
  /// <returns>Current tenant ID or null for global context</returns>
  Guid? GetCurrentTenantId();

  /// <summary>
  /// Check if current context has tenant isolation enabled
  /// </summary>
  /// <returns>True if tenant isolation is active</returns>
  bool IsTenantIsolationEnabled();
}

/// <summary>
/// Implementation of tenant isolation service
/// </summary>
public class TenantIsolationService(
  ITenantContextService tenantContextService,
  IHttpContextAccessor httpContextAccessor,
  ILogger<TenantIsolationService> logger) : ITenantIsolationService {
  private readonly AsyncLocal<bool> _isolationDisabled = new();

  public IQueryable<T> ApplyTenantFilter<T>(IQueryable<T> query, Guid? tenantId, bool includeGlobal = true) where T : class, ITenantable {
    if (!IsTenantIsolationEnabled()) {
      return query; // Return unfiltered query if isolation is disabled
    }

    if (tenantId == null) {
      // If no tenant specified, only return global entities
      return query.Where(e => e.Tenant == null);
    }

    if (includeGlobal) {
      // Return entities that are global OR belong to the specified tenant
      return query.Where(e => e.Tenant == null || e.Tenant.Id == tenantId);
    }

    // Return only entities that belong to the specified tenant
    return query.Where(e => e.Tenant != null && e.Tenant.Id == tenantId);
  }

  public IDisposable DisableTenantIsolation() {
    return new TenantIsolationDisabler(_isolationDisabled);
  }

  public Guid? GetCurrentTenantId() {
    try {
      var user = httpContextAccessor.HttpContext?.User;
      var tenantHeader = httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-ID"].FirstOrDefault();

      // Use async method synchronously in this context - this is acceptable for query filters
      // In practice, you might want to cache this value per request
      var task = tenantContextService.GetCurrentTenantIdAsync(user, tenantHeader);
      if (task.Wait(TimeSpan.FromSeconds(5))) {
        return task.Result;
      }

      logger.LogWarning("Timeout getting current tenant ID for query filter");
      return null;
    }
    catch (Exception ex) {
      logger.LogWarning(ex, "Failed to get current tenant ID for query filter");
      return null;
    }
  }

  public bool IsTenantIsolationEnabled() {
    return !_isolationDisabled.Value;
  }

  /// <summary>
  /// Helper class to temporarily disable tenant isolation
  /// </summary>
  private class TenantIsolationDisabler(AsyncLocal<bool> isolationDisabled) : IDisposable {
    private readonly bool _previousValue = isolationDisabled.Value;

    public void Dispose() {
      isolationDisabled.Value = _previousValue;
    }
  }
}