namespace GameGuild.Modules.Permissions;

/// <summary> User role assignment - simple mapping of user to role template </summary>
[Table("SimpleUserRoleAssignments")]
public class SimpleUserRoleAssignment : EntityBase
{
    [Required]
    public Guid UserId { get; set; }

    public Guid? TenantId { get; set; }

    [Required]
    [MaxLength(100)]
    public string RoleTemplateName { get; set; } = string.Empty; // "TestingLabAdmin", "ProjectManager", etc.

    public DateTime? ExpiresAt { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary> Navigation property to the role template </summary>
    public virtual RoleTemplate? RoleTemplate { get; set; }
}