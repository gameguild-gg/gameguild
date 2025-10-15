using GameGuild.Modules.Permissions;


namespace GameGuild.Modules.Tenants;

/// <summary>
/// Unified entity for tenant permissions and user-tenant relationships
/// Tenant-wide permissions for users, or default permissions for tenants
/// </summary>
[Table("TenantPermissions")]
[Index(nameof(UserId), nameof(TenantId), IsUnique = true, Name = "IX_TenantPermissions_User_Tenant")]
[Index(nameof(TenantId), Name = "IX_TenantPermissions_TenantId")]
[Index(nameof(UserId), Name = "IX_TenantPermissions_UserId")]
[Index(nameof(ExpiresAt), Name = "IX_TenantPermissions_ExpiresAt")]
public class TenantPermission : WithPermissions {
  // Computed properties specific to tenant permissions

  /// <summary>
  /// Whether this represents an active membership
  /// </summary>
  public bool IsActiveMembership {
    get => IsValid && UserId != null && TenantId != null;
  }
}
