namespace GameGuild.Modules.Permissions;

/// <summary> Tenant-wide permissions (Layer 1 of the DAC permission system) Allows setting permissions at the tenant level for users </summary>
[Table("TenantPermissions")]
[Index(nameof(UserId), nameof(TenantId), IsUnique = true, Name = "IX_TenantPermissions_User_Tenant")]
[Index(nameof(TenantId), Name = "IX_TenantPermissions_TenantId")]
[Index(nameof(UserId), Name = "IX_TenantPermissions_UserId")]
[Index(nameof(ExpiresAt), Name = "IX_TenantPermissions_ExpiresAt")]
public class TenantPermission : WithPermissions
{
    /// <summary> Default parameterless constructor (required by Entity Framework) </summary>
    public TenantPermission() { }

    /// <summary> Constructor for creating a tenant permission </summary>
    public TenantPermission(Guid? userId, Guid? tenantId) : base(userId, tenantId) { }
}