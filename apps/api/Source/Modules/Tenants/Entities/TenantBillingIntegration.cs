namespace GameGuild.Modules.Tenants;

/// <summary>
/// Represents a usage record for tenant billing and cost allocation.
/// </summary>
[Table("TenantUsageRecords")]
[Index(nameof(TenantId), nameof(RecordedAt), IsUnique = false)]
[Index(nameof(UsageType), nameof(RecordedAt), IsUnique = false)]
public class TenantUsageRecord
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Type of usage being tracked (e.g., StorageGB, APICallsCount, ComputeHours).
    /// </summary>
    [Required]
    public TenantUsageType UsageType { get; set; }

    /// <summary>
    /// Quantity of usage (e.g., GB, count, hours).
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,4)")]
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measurement (e.g., GB, requests, hours).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Cost per unit at the time of recording.
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal CostPerUnit { get; set; }

    /// <summary>
    /// Total cost for this usage record.
    /// </summary>
    [Column(TypeName = "decimal(18,8)")]
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Currency code (e.g., USD, EUR).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Resource identifier (e.g., database ID, API endpoint).
    /// </summary>
    [MaxLength(200)]
    public string? ResourceIdentifier { get; set; }

    /// <summary>
    /// Additional metadata about the usage.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When the usage was recorded.
    /// </summary>
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Billing period this usage belongs to.
    /// </summary>
    [MaxLength(20)]
    public string? BillingPeriod { get; set; }

    /// <summary>
    /// Whether this usage has been billed.
    /// </summary>
    public bool IsBilled { get; set; } = false;

    /// <summary>
    /// When this usage was billed.
    /// </summary>
    public DateTime? BilledAt { get; set; }

    /// <summary>
    /// Calculates the total cost based on quantity and cost per unit.
    /// </summary>
    public void CalculateCost()
    {
        TotalCost = Quantity * CostPerUnit;
    }

    /// <summary>
    /// Marks the usage as billed.
    /// </summary>
    public void MarkAsBilled()
    {
        IsBilled = true;
        BilledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the usage record.
    /// </summary>
    public void Validate()
    {
        if (Quantity < 0)
            throw new InvalidOperationException("Quantity cannot be negative");

        if (CostPerUnit < 0)
            throw new InvalidOperationException("Cost per unit cannot be negative");

        if (string.IsNullOrWhiteSpace(Unit))
            throw new InvalidOperationException("Unit is required");

        if (string.IsNullOrWhiteSpace(Currency))
            throw new InvalidOperationException("Currency is required");
    }
}

/// <summary>
/// Types of usage that can be tracked for billing.
/// </summary>
public enum TenantUsageType
{
    StorageGB = 1,
    APICallsCount = 2,
    ComputeHours = 3,
    DatabaseQueries = 4,
    BandwidthGB = 5,
    UsersCount = 6,
    TransactionsCount = 7,
    EmailsSent = 8,
    SMSSent = 9,
    BackupGB = 10,
    LogStorageGB = 11,
    EncryptionOperations = 12
}

/// <summary>
/// Represents billing integration configuration for a tenant.
/// </summary>
[Table("TenantBillingIntegrations")]
[Index(nameof(TenantId), IsUnique = true)]
public class TenantBillingIntegration
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public Guid TenantId { get; set; }

    /// <summary>
    /// Billing provider (e.g., Stripe, PayPal, Custom).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID in the billing provider's system.
    /// </summary>
    [MaxLength(100)]
    public string? CustomerId { get; set; }

    /// <summary>
    /// Subscription ID in the billing provider's system.
    /// </summary>
    [MaxLength(100)]
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Whether billing is active for this tenant.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Billing cycle (e.g., monthly, quarterly, annual).
    /// </summary>
    [Required]
    public TenantBillingCycle BillingCycle { get; set; } = TenantBillingCycle.Monthly;

    /// <summary>
    /// Next billing date.
    /// </summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>
    /// Payment method on file.
    /// </summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>
    /// Last 4 digits of payment method (for display).
    /// </summary>
    [MaxLength(4)]
    public string? PaymentMethodLast4 { get; set; }

    /// <summary>
    /// Current billing tier/plan.
    /// </summary>
    [MaxLength(50)]
    public string? CurrentPlan { get; set; }

    /// <summary>
    /// Configuration for cost allocation rules.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, decimal> CostAllocationRules { get; set; } = new();

    /// <summary>
    /// Metadata about billing integration.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// When the billing integration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the billing integration was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Activates billing for the tenant.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates billing for the tenant.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the next billing date based on the billing cycle.
    /// </summary>
    public void UpdateNextBillingDate()
    {
        var baseDate = NextBillingDate ?? DateTime.UtcNow;

        NextBillingDate = BillingCycle switch
        {
            TenantBillingCycle.Monthly => baseDate.AddMonths(1),
            TenantBillingCycle.Quarterly => baseDate.AddMonths(3),
            TenantBillingCycle.Annual => baseDate.AddYears(1),
            _ => baseDate.AddMonths(1)
        };

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Validates the billing integration configuration.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Provider))
            throw new InvalidOperationException("Provider is required");

        if (IsActive && string.IsNullOrWhiteSpace(CustomerId))
            throw new InvalidOperationException("Customer ID is required for active billing");
    }
}

/// <summary>
/// Billing cycle options for tenants.
/// </summary>
public enum TenantBillingCycle
{
    Monthly = 1,
    Quarterly = 2,
    Annual = 3
}
