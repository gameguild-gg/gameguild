using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Payments.Entities;

/// <summary>Entity representing a pricing rule</summary>
[Table("pricing_rules")]
[Index(nameof(ProductId))]
[Index(nameof(RuleType))]
[Index(nameof(IsActive))]
[Index(nameof(StartDate))]
[Index(nameof(EndDate))]
[Index(nameof(Priority))]
public abstract class PricingRule : EntityBase
{
    /// <summary>Default constructor</summary>
    public PricingRule() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial rule data</param>
    public PricingRule(object partial) : base(partial) { }

    /// <summary>Pricing rule type</summary>
    public PricingRuleType RuleType { get; set; }

    /// <summary>Product ID this rule applies to</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Rule name</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Rule description</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Discount percentage</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>Discount amount</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>Minimum quantity</summary>
    public int? MinQuantity { get; set; }

    /// <summary>Maximum quantity</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Buy quantity (for Buy X Get Y)</summary>
    public int? BuyQuantity { get; set; }

    /// <summary>Get quantity (for Buy X Get Y)</summary>
    public int? GetQuantity { get; set; }

    /// <summary>Rule start date</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Rule end date</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Rule priority (higher = applied first)</summary>
    public int Priority { get; set; }

    /// <summary>Whether this rule is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Navigation property to pricing tiers</summary>
    public virtual ICollection<PricingTier> PricingTiers { get; } = new List<PricingTier>();

    /// <summary>Check if rule is applicable</summary>
    public bool IsApplicable(int quantity, DateTime date)
    {
        if (!IsActive) return false;
        if (StartDate.HasValue && date < StartDate.Value) return false;
        if (EndDate.HasValue && date > EndDate.Value) return false;
        if (MinQuantity.HasValue && quantity < MinQuantity.Value) return false;
        if (MaxQuantity.HasValue && quantity > MaxQuantity.Value) return false;

        return true;
    }

    /// <summary>Calculate discount for this rule</summary>
    public decimal CalculateDiscount(decimal originalPrice, int quantity)
    {
        return RuleType switch
        {
            PricingRuleType.Percentage => originalPrice * quantity * (DiscountPercentage ?? 0) / 100,
            PricingRuleType.FixedAmount => DiscountAmount ?? 0,
            PricingRuleType.BuyXGetY => CalculateBuyXGetYDiscount(originalPrice, quantity),
            PricingRuleType.VolumeDiscount => CalculateVolumeDiscount(originalPrice, quantity),
            PricingRuleType.TieredPricing => CalculateVolumeDiscount(originalPrice, quantity),
            PricingRuleType.Bundle => DiscountAmount ?? 0,
            _ => 0
        };
    }

    private decimal CalculateBuyXGetYDiscount(decimal originalPrice, int quantity)
    {
        if (!BuyQuantity.HasValue || !GetQuantity.HasValue) return 0;

        var sets = quantity / (BuyQuantity.Value + GetQuantity.Value);
        var freeItems = sets * GetQuantity.Value;

        return freeItems * originalPrice;
    }

    private decimal CalculateVolumeDiscount(decimal originalPrice, int quantity)
    {
        var applicableTier = PricingTiers.Where(t => (!t.MinQuantity.HasValue || quantity >= t.MinQuantity.Value) && (!t.MaxQuantity.HasValue || quantity <= t.MaxQuantity.Value))
            .OrderByDescending(t => t.DiscountPercentage)
            .FirstOrDefault();

        if (applicableTier == null) return 0;

        if (applicableTier.Price.HasValue) return (originalPrice - applicableTier.Price.Value) * quantity;

        if (applicableTier.DiscountPercentage.HasValue) return originalPrice * quantity * applicableTier.DiscountPercentage.Value / 100;

        return 0;
    }
}
