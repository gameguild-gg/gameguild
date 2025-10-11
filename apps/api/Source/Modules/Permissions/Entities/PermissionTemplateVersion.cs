namespace GameGuild.Modules.Permissions.Entities;

/// <summary>
/// Represents a version of a permission template with change tracking
/// </summary>
[Table("PermissionTemplateVersions")]
[Index(nameof(TemplateId), Name = "IX_PermissionTemplateVersions_TemplateId")]
[Index(nameof(Version), Name = "IX_PermissionTemplateVersions_Version")]
[Index(nameof(IsActive), Name = "IX_PermissionTemplateVersions_IsActive")]
[Index(nameof(CreatedAt), Name = "IX_PermissionTemplateVersions_CreatedAt")]
public class PermissionTemplateVersion : EntityBase
{
    /// <summary>
    /// Reference to the parent template
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Version number (incremental)
    /// </summary>
    public override int Version { get; set; }

    /// <summary>
    /// Template name at this version
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Description at this version
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = null!;

    /// <summary>
    /// Permissions at this version
    /// </summary>
    public PermissionType[] Permissions { get; set; } = Array.Empty<PermissionType>();

    /// <summary>
    /// Whether this is the active version
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// User who created this version
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Change notes/description
    /// </summary>
    [MaxLength(1000)]
    public string? ChangeNotes { get; set; }

    /// <summary>
    /// Type of change
    /// </summary>
    public TemplateChangeType ChangeType { get; set; }

    /// <summary>
    /// Permissions added in this version (compared to previous)
    /// </summary>
    public PermissionType[]? AddedPermissions { get; set; }

    /// <summary>
    /// Permissions removed in this version (compared to previous)
    /// </summary>
    public PermissionType[]? RemovedPermissions { get; set; }

    /// <summary>
    /// Permissions that remained unchanged
    /// </summary>
    public PermissionType[]? UnchangedPermissions { get; set; }

    /// <summary>
    /// Previous version number (null for version 1)
    /// </summary>
    public int? PreviousVersion { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Checksum/hash of the permission set for integrity
    /// </summary>
    [MaxLength(64)]
    public string? PermissionHash { get; set; }

    /// <summary>
    /// Tags for categorization and search
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Calculate a hash of the permission set
    /// </summary>
    public static string CalculatePermissionHash(PermissionType[] permissions)
    {
        var ordered = permissions.OrderBy(p => p).Select(p => (int)p).ToArray();
        var hashString = string.Join(",", ordered);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(hashString));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Compare with another version to find differences
    /// </summary>
    public VersionDiff CompareWith(PermissionTemplateVersion other)
    {
        var added = Permissions.Except(other.Permissions).ToArray();
        var removed = other.Permissions.Except(Permissions).ToArray();
        var unchanged = Permissions.Intersect(other.Permissions).ToArray();

        return new VersionDiff
        {
            FromVersion = other.Version,
            ToVersion = Version,
            AddedPermissions = added,
            RemovedPermissions = removed,
            UnchangedPermissions = unchanged,
            TotalChanges = added.Length + removed.Length
        };
    }
}

/// <summary>
/// Type of change in a template version
/// </summary>
public enum TemplateChangeType
{
    Created = 0,
    PermissionsAdded = 1,
    PermissionsRemoved = 2,
    PermissionsModified = 3,
    MetadataChanged = 4,
    Renamed = 5,
    Deprecated = 6,
    Restored = 7
}

/// <summary>
/// Difference between two template versions
/// </summary>
public class VersionDiff
{
    public int FromVersion { get; set; }
    public int ToVersion { get; set; }
    public PermissionType[] AddedPermissions { get; set; } = Array.Empty<PermissionType>();
    public PermissionType[] RemovedPermissions { get; set; } = Array.Empty<PermissionType>();
    public PermissionType[] UnchangedPermissions { get; set; } = Array.Empty<PermissionType>();
    public int TotalChanges { get; set; }

    public bool HasChanges => TotalChanges > 0;
    public bool IsBreakingChange => RemovedPermissions.Length > 0;
}
