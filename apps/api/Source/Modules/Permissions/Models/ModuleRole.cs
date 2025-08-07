using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GameGuild.Common;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Permissions;

/// <summary>
/// Module-specific role definition that contains a set of permissions for a specific module
/// </summary>
[Table("ModuleRoles")]
public class ModuleRole : BaseEntity
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public ModuleType Module { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// JSON-serialized list of ModulePermissionDefinition objects
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string PermissionsJson { get; set; } = "[]";

    /// <summary>
    /// Permissions defined in this module role
    /// </summary>
    [NotMapped]
    public List<ModulePermissionDefinition> Permissions
    {
        get => string.IsNullOrEmpty(PermissionsJson) 
            ? new List<ModulePermissionDefinition>() 
            : JsonSerializer.Deserialize<List<ModulePermissionDefinition>>(PermissionsJson) ?? new List<ModulePermissionDefinition>();
        set => PermissionsJson = JsonSerializer.Serialize(value);
    }

    public int Priority { get; set; } = 0;
    public bool IsSystemRole { get; set; } = false;

    // Tenant support (TenantId is in BaseEntity as part of ITenantable)
    public Guid? TenantId { get; set; }

    // Navigation properties
    public virtual ICollection<UserRoleAssignment> UserRoleAssignments { get; } = new List<UserRoleAssignment>();
}

/// <summary>
/// Configuration for ModuleRole entity
/// </summary>
public class ModuleRoleConfiguration : IEntityTypeConfiguration<ModuleRole>
{
    public void Configure(EntityTypeBuilder<ModuleRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        builder.HasIndex(x => new { x.Name, x.Module, x.TenantId }).IsUnique();
        
        builder.HasMany(x => x.UserRoleAssignments)
               .WithOne(x => x.Role)
               .HasForeignKey(x => x.RoleId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
