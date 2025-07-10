using System.Security.Claims;
using GameGuild.Modules.Tenants.Entities;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Service for managing tenant context in requests
/// </summary>
public interface ITenantContextService {
  /// <summary>
  /// Get the current tenant ID from the request or claims
  /// </summary>
  Task<Guid?> GetCurrentTenantIdAsync(ClaimsPrincipal? user = null, string? tenantHeader = null);

  /// <summary>
  /// Get the current tenant entity
  /// </summary>
  Task<Tenant?> GetCurrentTenantAsync(ClaimsPrincipal? user = null, string? tenantHeader = null);

  /// <summary>
  /// Get permission data for user in the specified tenant
  /// </summary>
  Task<TenantPermission?> GetTenantPermissionAsync(Guid userId, Guid tenantId);

  /// <summary>
  /// Check if user has permission to access the specified tenant
  /// </summary>
  Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, Guid tenantId);
}
