using System.Security.Claims;
using GameGuild.Modules.Auth.Dtos;
using GameGuild.Modules.Tenant.Models;
using GameGuild.Modules.Tenant.Services;
using GameGuild.Modules.Auth.Constants;


namespace GameGuild.Modules.Auth.Services;

/// <summary>
/// Service for enhancing authentication with tenant-specific operations
/// </summary>
public interface ITenantAuthService {
  /// <summary>
  /// Validate user's tenant access and include tenant data in authentication result
  /// </summary>
  Task<SignInResponseDto> EnhanceWithTenantDataAsync(
    SignInResponseDto authResult,
    GameGuild.Modules.User.Models.User user, Guid? tenantId = null
  );

  /// <summary>
  /// Get tenant-specific claims for a user
  /// </summary>
  Task<IEnumerable<Claim>> GetTenantClaimsAsync(GameGuild.Modules.User.Models.User user, Guid tenantId);

  /// <summary>
  /// Get all available tenants for a user
  /// </summary>
  Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(GameGuild.Modules.User.Models.User user);
}

/// <summary>
/// Implementation of tenant auth service
/// </summary>
public class TenantAuthService(
  ITenantService tenantService,
  ITenantContextService tenantContextService,
  IJwtTokenService jwtTokenService
) : ITenantAuthService {
  /// <summary>
  /// Enhance authentication result with tenant data
  /// </summary>
  public async Task<SignInResponseDto> EnhanceWithTenantDataAsync(
    SignInResponseDto authResult,
    GameGuild.Modules.User.Models.User user, Guid? tenantId = null
  ) {
    // Get available tenants for the user
    var tenantPermissions = await tenantService.GetTenantsForUserAsync(user.Id);
    var availableTenants = tenantPermissions.Where(tp => tp.IsValid).ToList();

    // If no tenants available, return original result
    if (!availableTenants.Any()) { return authResult; }

    // Use specified tenant ID or the first available tenant
    Guid selectedTenantId;

    if (tenantId.HasValue) { selectedTenantId = tenantId.Value; }
    else if (availableTenants.First().TenantId.HasValue) { selectedTenantId = availableTenants.First().TenantId.Value; }
    else {
      // If we somehow don't have a valid tenant ID, return original result
      return authResult;
    }

    // Validate tenant access
    TenantPermission? tenantPermission =
      availableTenants.FirstOrDefault(tp => tp.TenantId.HasValue && tp.TenantId.Value == selectedTenantId);

    if (tenantPermission == null) {
      // If specified tenant is not accessible, use the first available
      tenantPermission = availableTenants.First();

      if (tenantPermission.TenantId.HasValue) { selectedTenantId = tenantPermission.TenantId.Value; }
      else {
        // If we somehow don't have a valid tenant ID, return original result
        return authResult;
      }
    }

    // Get tenant claims
    var tenantClaims = await GetTenantClaimsAsync(user, selectedTenantId);

    // Generate new token with tenant claims
    var userDto = new UserDto { Id = user.Id, Username = user.Name, Email = user.Email };
    var roles = new[] { "User" }; // TODO: fetch actual tenant-specific roles
    string accessToken = jwtTokenService.GenerateAccessToken(userDto, roles, tenantClaims);

    // Update response with new token and tenant info
    authResult.AccessToken = accessToken;
    authResult.TenantId = selectedTenantId;
    authResult.AvailableTenants = availableTenants.Where(tp => tp.TenantId.HasValue)
                                                  .Select(tp => new TenantInfoDto { Id = tp.TenantId.Value, Name = tp.Tenant?.Name ?? "Unknown Tenant", IsActive = tp.Tenant?.IsActive ?? false })
                                                  .ToList();

    return authResult;
  }

  /// <summary>
  /// Get tenant-specific claims for a user
  /// </summary>
  public async Task<IEnumerable<Claim>> GetTenantClaimsAsync(GameGuild.Modules.User.Models.User user, Guid tenantId) {
    var claims = new List<Claim> { new Claim(JwtClaimTypes.TenantId, tenantId.ToString()), };

    // Get tenant permission for additional claims
    TenantPermission? tenantPermission = await tenantContextService.GetTenantPermissionAsync(user.Id, tenantId);

    // Add permission flags if available
    if (tenantPermission != null) {
      claims.Add(new Claim(JwtClaimTypes.TenantPermissionFlags1, tenantPermission.PermissionFlags1.ToString()));
      claims.Add(new Claim(JwtClaimTypes.TenantPermissionFlags2, tenantPermission.PermissionFlags2.ToString()));
    }

    return claims;
  }

  /// <summary>
  /// Get all available tenants for a user
  /// </summary>
  public async Task<IEnumerable<TenantPermission>> GetUserTenantsAsync(GameGuild.Modules.User.Models.User user) { return await tenantService.GetTenantsForUserAsync(user.Id); }
}
