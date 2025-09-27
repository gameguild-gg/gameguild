using System.Text.Json;

namespace GameGuild.Modules.Permissions;

/// <summary> Simple role template that defines what permissions a user gets </summary>
[Table("RoleTemplates")]
public class RoleTemplate : EntityBase
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary> Serialized permission templates as JSON Example: [{"action": "read", "resourceType": "TestingSession"}, {"action": "create", "resourceType": "TestingSession"}] </summary>
    [Column(TypeName = "jsonb")]
    public string PermissionTemplatesJson { get; set; } = "[]";

    /// <summary> Permission templates for this role </summary>
    [NotMapped]
    public List<PermissionTemplate> PermissionTemplates
    {
        get => string.IsNullOrEmpty(PermissionTemplatesJson) ? [] : JsonSerializer.Deserialize<List<PermissionTemplate>>(PermissionTemplatesJson) ?? [];
        set => PermissionTemplatesJson = JsonSerializer.Serialize(value);
    }

    public bool IsSystemRole { get; set; } // System roles cannot be modified

    /// <summary> Navigation property to user assignments </summary>
    public virtual ICollection<UserPermission> UserPermissions { get; set; } = [];
}