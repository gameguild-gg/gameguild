namespace GameGuild.Modules.Products.Domain.Entities;

/// <summary>Entity representing dynamic pricing rules</summary>
[Table("pricing_rules")]
[Index(nameof(ProductId))]
[Index(nameof(RuleType))]
[Index(nameof(IsActive))]
[Index(nameof(Priority))]
[Index(nameof(StartDate))]
[Index(nameof(EndDate))]
public class PricingRule : EntityBase
{
    /// <summary>Default constructor</summary>
    public PricingRule() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial pricing rule data</param>
    public PricingRule(object partial) : base(partial) { }

    /// <summary>Foreign key to the Product entity</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the Product entity</summary>
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    /// <summary>Name of the pricing rule</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the pricing rule</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Type of pricing rule</summary>
    public PricingRuleType RuleType { get; set; }

    /// <summary>Priority of the rule (higher = applied first)</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Whether this rule is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>When this rule becomes active</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>When this rule expires</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Minimum quantity required for volume discount</summary>
    public int? MinQuantity { get; set; }

    /// <summary>Maximum quantity for volume discount</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Discount percentage for volume pricing</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>Fixed price override</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? FixedPrice { get; set; }

    /// <summary>Time-based pricing start time (HH:MM)</summary>
    [MaxLength(5)]
    public string? TimeStart { get; set; }

    /// <summary>Time-based pricing end time (HH:MM)</summary>
    [MaxLength(5)]
    public string? TimeEnd { get; set; }

    /// <summary>Days of week for time-based pricing (comma-separated: 0-6)</summary>
    [MaxLength(20)]
    public string? DaysOfWeek { get; set; }

    /// <summary>Geographic region for region-based pricing</summary>
    [MaxLength(100)]
    public string? Region { get; set; }

    /// <summary>Customer segment for segment-based pricing</summary>
    [MaxLength(100)]
    public string? CustomerSegment { get; set; }

    /// <summary>Check if the pricing rule is currently applicable</summary>
    public bool IsApplicable(DateTime? checkDate = null)
    {
        if (!IsActive) return false;

        var now = checkDate ?? DateTime.UtcNow;

        return (StartDate == null || StartDate <= now) && (EndDate == null || EndDate > now);
    }

    /// <summary>Check if the rule applies to the given quantity</summary>
    public bool AppliesToQuantity(int quantity)
    {
        if (!IsApplicable()) return false;

        return (MinQuantity == null || quantity >= MinQuantity) && (MaxQuantity == null || quantity <= MaxQuantity);
    }

    /// <summary>Calculate the price using this rule</summary>
    public decimal CalculatePrice(decimal basePrice, int quantity)
    {
        if (!AppliesToQuantity(quantity)) return basePrice;

        return RuleType switch
        {
            PricingRuleType.VolumeDiscount => basePrice * (1 - (DiscountPercentage ?? 0) / 100),
            PricingRuleType.FixedPriceOverride => FixedPrice ?? basePrice,
            PricingRuleType.TimeBased => basePrice * (1 - (DiscountPercentage ?? 0) / 100),
            PricingRuleType.RegionBased => FixedPrice ?? (basePrice * (1 - (DiscountPercentage ?? 0) / 100)),
            PricingRuleType.SegmentBased => basePrice * (1 - (DiscountPercentage ?? 0) / 100),
            _ => basePrice
        };
    }
}

/// <summary>Types of pricing rules</summary>
public enum PricingRuleType
{
    /// <summary>Volume-based discount</summary>
    VolumeDiscount = 0,

    /// <summary>Fixed price override</summary>
    FixedPriceOverride = 1,

    /// <summary>Time-based pricing (happy hour, seasonal)</summary>
    TimeBased = 2,

    /// <summary>Region-based pricing</summary>
    RegionBased = 3,

    /// <summary>Customer segment-based pricing</summary>
    SegmentBased = 4,

    /// <summary>Dynamic market-based pricing</summary>
    MarketBased = 5
}
