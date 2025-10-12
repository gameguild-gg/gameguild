using GameGuild.Modules.Permissions.Constants;

namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Permission templates for quick role assignment and management
/// </summary>
[Table("PermissionTemplates")]
[Index(nameof(Name), IsUnique = true, Name = "IX_PermissionTemplates_Name")]
[Index(nameof(Module), Name = "IX_PermissionTemplates_Module")]
[Index(nameof(IsSystemTemplate), Name = "IX_PermissionTemplates_IsSystemTemplate")]
public class PermissionTemplate : EntityBase {
    /// <summary>
    /// Template name (must be unique)
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Description of what this template provides
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Permissions included in this template
    /// </summary>
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Module this template applies to (optional)
    /// </summary>
    public ModuleType? Module { get; set; }

    /// <summary>
    /// Whether this is a system-defined template (cannot be modified)
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Whether this template is active and available for use
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Template category for organization
    /// </summary>
    [MaxLength(50)]
    public string? Category { get; set; }

    /// <summary>
    /// Minimum tenant tier required to use this template
    /// </summary>
    [MaxLength(50)]
    public string? MinimumTier { get; set; }

    /// <summary>
    /// Additional metadata for the template
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    // Backward compatibility properties
    public string ResourceType { get; set; } = "*";
    public string Action { get; set; } = "read";

    /// <summary>
    /// Predefined system templates
    /// </summary>
    public static class SystemTemplates {
        public static readonly PermissionTemplate TenantAdmin = new() {
            Name = "TenantAdmin",
            Description = "Full administrative access to tenant resources",
            Permissions = TenantPermissionConstants.AdminPermissions,
            IsSystemTemplate = true,
            Category = "Administrative"
        };

        public static readonly PermissionTemplate TenantModerator = new() {
            Name = "TenantModerator",
            Description = "Moderation capabilities within tenant",
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Vote,
                PermissionType.Share,
                PermissionType.Report,
                PermissionType.Review,
                PermissionType.Flag,
                PermissionType.Hide,
                PermissionType.Pin,
                PermissionType.Ban
            },
            IsSystemTemplate = true,
            Category = "Moderation"
        };

        public static readonly PermissionTemplate ContentCreator = new() {
            Name = "ContentCreator",
            Description = "Create and manage content within tenant",
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Vote,
                PermissionType.Share,
                PermissionType.Create,
                PermissionType.Edit,
                PermissionType.Delete,
                PermissionType.Publish,
                PermissionType.Draft,
                PermissionType.Schedule,
                PermissionType.Tag,
                PermissionType.Categorize
            },
            IsSystemTemplate = true,
            Category = "Content"
        };

        public static readonly PermissionTemplate BasicUser = new() {
            Name = "BasicUser",
            Description = "Basic user permissions for tenant participation",
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Reply,
                PermissionType.Vote,
                PermissionType.Share,
                PermissionType.Bookmark,
                PermissionType.React,
                PermissionType.Follow
            },
            IsSystemTemplate = true,
            Category = "Basic"
        };

        public static readonly PermissionTemplate ReadOnly = new() {
            Name = "ReadOnly",
            Description = "Read-only access to public content",
            Permissions = new[]
            {
                PermissionType.Read
            },
            IsSystemTemplate = true,
            Category = "Basic"
        };

        public static readonly PermissionTemplate QualityAssurance = new() {
            Name = "QualityAssurance",
            Description = "Quality control and review permissions",
            Permissions = new[]
            {
                PermissionType.Read,
                PermissionType.Comment,
                PermissionType.Vote,
                PermissionType.Review,
                PermissionType.Approve,
                PermissionType.Reject,
                PermissionType.Audit,
                PermissionType.Flag,
                PermissionType.Report
            },
            IsSystemTemplate = true,
            Category = "Quality"
        };

        /// <summary>
        /// Get all system templates
        /// </summary>
        public static PermissionTemplate[] GetAll() {
            return new[]
            {
                TenantAdmin,
                TenantModerator,
                ContentCreator,
                BasicUser,
                ReadOnly,
                QualityAssurance
            };
        }
    }
}
