
namespace GameGuild.Identity.Authentication;

/// <summary>
///     Service for tenant-specific authentication operations.
///     Handles tenant isolation, tenant-specific auth policies, and cross-tenant restrictions.
/// </summary>
public interface ITenantAuthService
{
    /// <summary>
    ///     Validates that a user can authenticate within a specific tenant context.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>True if user is authorized for the tenant</returns>
    Task<bool> ValidateTenantAccessAsync(Guid userId, Guid tenantId);

    /// <summary>
    ///     Gets tenant-specific authentication configuration and policies.
    /// </summary>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Tenant auth configuration</returns>
    Task<TenantAuthConfiguration?> GetTenantAuthConfigurationAsync(Guid tenantId);

    /// <summary>
    ///     Adds tenant-specific claims to authentication tokens.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="tenantId">The tenant ID</param>
    /// <returns>Dictionary of tenant-specific claims</returns>
    Task<Dictionary<string, object>> GetTenantClaimsAsync(Guid userId, Guid tenantId);
}
