namespace GameGuild.Modules.Resources;

/// <summary>
///     Tags for resource ownership, cost allocation, and governance
/// </summary>
[Table("resource_tags")]
[Index(nameof(ResourceQuotaId), nameof(Key), IsUnique = true)]
public class ResourceTag : EntityBase
{
    /// <summary>
    ///     Associated resource quota
    /// </summary>
    public Guid ResourceQuotaId { get; set; }

    /// <summary>
    ///     Tag key (e.g., "CostCenter", "Department", "Project", "Owner")
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Key { get; set; } = null!;

    /// <summary>
    ///     Tag value (e.g., "Engineering", "john.doe@example.com")
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Value { get; set; } = null!;

    /// <summary>
    ///     Tag category for grouping (e.g., "Cost", "Governance", "Security")
    /// </summary>
    [MaxLength(100)]
    public string? Category { get; set; }

    /// <summary>
    ///     Whether this tag is required by governance policy
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    ///     Whether this tag should be included in cost allocation reports
    /// </summary>
    public bool IncludeInCostAllocation { get; set; } = true;

    /// <summary>
    ///     Navigation property to resource quota
    /// </summary>
    public virtual ResourceQuota ResourceQuota { get; set; } = null!;
}
