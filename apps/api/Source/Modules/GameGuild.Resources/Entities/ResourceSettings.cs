using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Resources;

/// <summary>
///     Stores configuration settings for resource management
/// </summary>
[Table("ResourceSettings")]
public class ResourceSettings : EntityBase
{
    /// <summary>
    ///     Unique key/name for the setting
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    ///     Value of the setting (JSON)
    /// </summary>
    [MaxLength(4000)]
    public string? Value { get; set; }

    /// <summary>
    ///     Default value if no override is set
    /// </summary>
    [MaxLength(4000)]
    public string? DefaultValue { get; set; }

    /// <summary>
    ///     Data type of the value (String, Number, Boolean, JSON, etc.)
    /// </summary>
    [MaxLength(50)]
    public string DataType { get; set; } = "String";

    /// <summary>
    ///     Human-readable description of this setting
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    ///     Category for grouping related settings
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    ///     Whether this setting is system-managed (read-only for users)
    /// </summary>
    public bool IsSystemManaged { get; set; }

    /// <summary>
    ///     Whether this setting is active/enabled
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    ///     Whether users can override this setting at user level
    /// </summary>
    public bool AllowUserOverride { get; set; } = true;

    /// <summary>
    ///     Display order for UI purposes
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    ///     User ID if this is a user-level setting override (null for tenant/global)
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    ///     Validation rules for the setting value (JSON schema or regex)
    /// </summary>
    [MaxLength(1000)]
    public string? ValidationRules { get; set; }

    /// <summary>
    ///     Row version for optimistic concurrency control
    /// </summary>
    [Timestamp]
    public byte[]? RowVersion { get; set; }

    /// <summary>
    ///     Gets the effective value (Value if set, otherwise DefaultValue)
    /// </summary>
    public string? GetEffectiveValue() => Value ?? DefaultValue;
}
