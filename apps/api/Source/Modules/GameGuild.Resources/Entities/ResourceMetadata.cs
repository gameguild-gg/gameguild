using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Resources;

/// <summary>
///     Stores metadata about resource types and their configurations
/// </summary>
[Table("ResourceMetadata")]
public class ResourceMetadata : EntityBase
{
    /// <summary>
    ///     Unique key/name for the metadata entry
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Value of the metadata entry (JSON)
    /// </summary>
    [MaxLength(4000)]
    public string? Value { get; set; }

    /// <summary>
    ///     Data type of the value (String, Number, Boolean, JSON, etc.)
    /// </summary>
    [MaxLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    ///     Human-readable description of this metadata
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Category for grouping related metadata entries
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    ///     Whether this metadata is system-managed (read-only for users)
    /// </summary>
    public bool IsSystemManaged { get; set; }

    /// <summary>
    ///     Whether this metadata entry is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Display order for UI purposes
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     User ID if this is user-specific metadata (null for tenant/global)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Resource ID if this metadata is for a specific resource
    /// </summary>
    public Guid? ResourceId { get; set; }

    /// <summary>
    ///     Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }
}
