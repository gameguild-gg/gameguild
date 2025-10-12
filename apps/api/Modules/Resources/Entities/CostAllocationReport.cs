namespace GameGuild.Modules.Resources.Entities;

/// <summary>
/// Cost allocation report for chargeback and billing integration
/// </summary>
[Table("cost_allocation_reports")]
public class CostAllocationReport : EntityBase
{
    /// <summary>
    /// Tenant ID for this cost allocation
    /// </summary>
    public new Guid? TenantId { get; set; }

    /// <summary>
    /// Reporting period start date
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Reporting period end date
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Usage type being reported
    /// </summary>
    public ResourceUsageType UsageType { get; set; }

    /// <summary>
    /// Total usage units for the period
    /// </summary>
    public long TotalUsage { get; set; }

    /// <summary>
    /// Cost per unit
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostPerUnit { get; set; }

    /// <summary>
    /// Total cost for the period
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost allocation tags (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AllocationTags { get; set; }

    /// <summary>
    /// Cost center or department
    /// </summary>
    [MaxLength(200)]
    public string? CostCenter { get; set; }

    /// <summary>
    /// Project or workload identifier
    /// </summary>
    [MaxLength(200)]
    public string? Project { get; set; }

    /// <summary>
    /// Owner or responsible party
    /// </summary>
    [MaxLength(200)]
    public string? Owner { get; set; }

    /// <summary>
    /// Whether the report has been exported/billed
    /// </summary>
    public bool IsExported { get; set; }

    /// <summary>
    /// Export date for billing system
    /// </summary>
    public DateTime? ExportedAt { get; set; }

    /// <summary>
    /// Invoice or billing reference ID
    /// </summary>
    [MaxLength(100)]
    public string? InvoiceReference { get; set; }

    /// <summary>
    /// Additional metadata (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }
}
