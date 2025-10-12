using System.Text.Json;

namespace GameGuild.Modules.Permissions;

/// <summary> User permission assignment - links user to specific resources with specific actions </summary>
[Table("UserPermissions")]
public class UserPermission : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public new Guid? TenantId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // "read", "create", "edit", "delete"

    [Required]
    [MaxLength(100)]
    public string ResourceType { get; set; } = string.Empty; // "TestingSession", "Project", etc.

    public Guid? ResourceId { get; set; } // Specific resource ID, null for type-level permissions

    [MaxLength(100)]
    public string? GrantedByRole { get; set; } // Which role template granted this permission

    /// <summary> Serialized constraints as JSON </summary>
    [Column(TypeName = "jsonb")]
    public string ConstraintsJson { get; set; } = "[]";

    /// <summary> Constraints for this permission </summary>
    [NotMapped]
    public List<PermissionConstraint> Constraints
    {
        get => string.IsNullOrEmpty(ConstraintsJson) ? [] : JsonSerializer.Deserialize<List<PermissionConstraint>>(ConstraintsJson) ?? [];
        set => ConstraintsJson = JsonSerializer.Serialize(value);
    }

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;
}
