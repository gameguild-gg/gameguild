using System.Security.Claims;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users;


namespace GameGuild.Modules.Authentication;

/// <summary>
/// Service for enhancing authentication with tenant-specific operations
/// </summary>
public interface ITenantAuthService {
  /// <summary>
  /// Validate user's tenant access and include tenant data in authentication result
  /// </summary>
  Task<SignInResponseDto> EnhanceWithTenantDataAsync(
    SignInResponseDto authResult,
    User user, Guid? tenantId = null
  );

  /// <summary>
  /// Get tenant-specific claims for a user
  /// </summary>
  Task<IEnumerable<Claim>> GetTenantClaimsAsync(User user, Guid tenantId);

  /// <summary>
  /// Get all available tenants for a user
  /// </summary>
  Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(User user);
}
