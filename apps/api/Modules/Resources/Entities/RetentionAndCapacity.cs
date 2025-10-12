namespace GameGuild.Modules.Resources.Entities;

/// <summary>
/// Usage retention and data lifecycle policy
/// </summary>
[Table("usage_retention_policies")]
public class UsageRetentionPolicy : EntityBase
{
    /// <summary>
    /// Tenant ID (null for global policies)
    /// </summary>
    // TenantId inherited from EntityBase (no override needed)

    /// <summary>
    /// Resource type this policy applies to
    /// </summary>
    public ResourceUsageType? ResourceType { get; set; }

    /// <summary>
    /// Policy name
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Retention period in days
    /// </summary>
    public int RetentionDays { get; set; } = 90;

    /// <summary>
    /// Archive after days (before deletion)
    /// </summary>
    public int ArchiveAfterDays { get; set; } = 30;

    /// <summary>
    /// Whether to enable data compaction
    /// </summary>
    public bool EnableCompaction { get; set; } = true;

    /// <summary>
    /// Compaction interval in days
    /// </summary>
    public int CompactionIntervalDays { get; set; } = 7;

    /// <summary>
    /// Down-sampling strategy (None, Hourly, Daily, Weekly)
    /// </summary>
    [MaxLength(50)]
    public string DownSamplingStrategy { get; set; } = "Daily";

    /// <summary>
    /// Minimum records to keep (even if beyond retention)
    /// </summary>
    public int MinimumRecordsToKeep { get; set; } = 1000;

    /// <summary>
    /// Whether policy is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority for conflict resolution
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Last execution timestamp
    /// </summary>
    public DateTime? LastExecutedAt { get; set; }

    /// <summary>
    /// Next scheduled execution
    /// </summary>
    public DateTime? NextExecutionAt { get; set; }

    /// <summary>
    /// Execution frequency (Daily, Weekly, Monthly)
    /// </summary>
    [MaxLength(50)]
    public string ExecutionFrequency { get; set; } = "Daily";

    /// <summary>
    /// Policy configuration (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Configuration { get; set; }
}

/// <summary>
/// Reserved capacity and commitment discounting model
/// </summary>
[Table("reserved_capacities")]
public class ReservedCapacity : EntityBase
{
    /// <summary>
    /// Tenant ID
    /// </summary>
    // TenantId inherited from EntityBase (no override needed)

    /// <summary>
    /// Resource type
    /// </summary>
    public ResourceUsageType ResourceType { get; set; }

    /// <summary>
    /// Reserved quantity
    /// </summary>
    public long ReservedQuantity { get; set; }

    /// <summary>
    /// Commitment term in months (1, 12, 36)
    /// </summary>
    public int CommitmentTermMonths { get; set; }

    /// <summary>
    /// Start date of reservation
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date of reservation
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Standard price per unit
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal StandardPricePerUnit { get; set; }

    /// <summary>
    /// Discounted price per unit
    /// </summary>
    [Column(TypeName = "decimal(18,4)")]
    public decimal DiscountedPricePerUnit { get; set; }

    /// <summary>
    /// Discount percentage
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal DiscountPercentage { get; set; }

    /// <summary>
    /// Total commitment amount
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCommitmentAmount { get; set; }

    /// <summary>
    /// Amount consumed to date
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal ConsumedAmount { get; set; }

    /// <summary>
    /// Units consumed to date
    /// </summary>
    public long ConsumedUnits { get; set; }

    /// <summary>
    /// Whether auto-renewal is enabled
    /// </summary>
    public bool AutoRenew { get; set; }

    /// <summary>
    /// Payment status (Pending, Active, Expired, Cancelled)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Purchase order or contract reference
    /// </summary>
    [MaxLength(200)]
    public string? ContractReference { get; set; }

    /// <summary>
    /// Billing frequency (Monthly, Quarterly, Annually, Upfront)
    /// </summary>
    [MaxLength(50)]
    public string BillingFrequency { get; set; } = "Monthly";

    /// <summary>
    /// Additional terms (JSON)
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? AdditionalTerms { get; set; }
}
