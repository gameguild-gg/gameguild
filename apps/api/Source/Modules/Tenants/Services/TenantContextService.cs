using System.Security.Claims;
using GameGuild.Database;
using GameGuild.Modules.Tenants.Entities;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Service implementation for tenant context management
/// </summary>
public class TenantContextService(ApplicationDbContext context) : ITenantContextService {
  private const string TenantClaim = "tenant_id";

  private const string TenantHeader = "X-Tenant-ID";

  /// <summary>
  /// Get the current tenant ID from the request or claims
  /// </summary>
  public async Task<Guid?> GetCurrentTenantIdAsync(ClaimsPrincipal? user = null, string? tenantHeader = null) {
    // Check header first (highest priority)
    if (!string.IsNullOrEmpty(tenantHeader) && Guid.TryParse(tenantHeader, out var tenantHeaderId))
      // Verify tenant exists
      if (await context.Tenants.AnyAsync(t => t.Id == tenantHeaderId && t.IsActive))
        return tenantHeaderId;

    // Check user claims
    if (user?.Identity?.IsAuthenticated == true) {
      var tenantClaim = user.FindFirst(TenantClaim);

      if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantClaimId))
        // Verify tenant exists
        if (await context.Tenants.AnyAsync(t => t.Id == tenantClaimId && t.IsActive))
          return tenantClaimId;
    }

    return null;
  }

  /// <summary>
  /// Get the current tenant entity 
  /// </summary>
  public async Task<Tenant?> GetCurrentTenantAsync(ClaimsPrincipal? user = null, string? tenantHeader = null) {
    var tenantId = await GetCurrentTenantIdAsync(user, tenantHeader);

    if (!tenantId.HasValue) return null;

    return await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId && t.IsActive);
  }

  /// <summary>
  /// Get permission data for user in the specified tenant
  /// </summary>
  public async Task<TenantPermission?> GetTenantPermissionAsync(Guid userId, Guid tenantId) {
    return await context.TenantPermissions.FirstOrDefaultAsync(tp =>
                                                                 tp.UserId == userId && tp.TenantId == tenantId && tp.IsValid
           );
  }

  /// <summary>
  /// Check if user has permission to access the specified tenant
  /// </summary>
  public async Task<bool> CanAccessTenantAsync(ClaimsPrincipal user, Guid tenantId) {
    if (!user.Identity?.IsAuthenticated == true ||
        string.IsNullOrEmpty(user.FindFirst(ClaimTypes.NameIdentifier)?.Value))
      return false;

    var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    var permission = await GetTenantPermissionAsync(userId, tenantId);

    return permission != null;
  }
}
