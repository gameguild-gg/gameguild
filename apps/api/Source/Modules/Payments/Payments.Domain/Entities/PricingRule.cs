using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Core.Domain;

namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Defines pricing rules for products</summary>
public class PricingRule : EntityBase
{
    /// <summary>Type of pricing rule</summary>
    [Required]
    public PricingRuleType RuleType { get; set; }

    /// <summary>Product this rule applies to</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Rule name</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Rule description</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Discount percentage (for percentage-based rules)</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>Discount amount (for fixed amount rules)</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>Minimum quantity required</summary>
    public int? MinQuantity { get; set; }

    /// <summary>Maximum quantity allowed</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Buy quantity (for Buy X Get Y rules)</summary>
    public int? BuyQuantity { get; set; }

    /// <summary>Get quantity (for Buy X Get Y rules)</summary>
    public int? GetQuantity { get; set; }

    /// <summary>Rule start date</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Rule end date</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Rule priority (higher = applied first)</summary>
    public int Priority { get; set; }

    /// <summary>Whether rule is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Pricing tiers for volume-based pricing</summary>
    public virtual ICollection<PricingTier> PricingTiers { get; set; } = new List<PricingTier>();

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

    /// <summary>Calculate discount based on rule</summary>
    public decimal CalculateDiscount(decimal originalPrice, int quantity)
    {
        return RuleType switch
        {
            PricingRuleType.Percentage => originalPrice * (DiscountPercentage ?? 0) / 100,
            PricingRuleType.FixedAmount => DiscountAmount ?? 0,
            PricingRuleType.BuyXGetY => CalculateBuyXGetYDiscount(originalPrice, quantity),
            PricingRuleType.VolumeDiscount => CalculateVolumeDiscount(originalPrice, quantity),
            _ => 0
        };
    }

    private decimal CalculateBuyXGetYDiscount(decimal originalPrice, int quantity)
    {
        if (!BuyQuantity.HasValue || !GetQuantity.HasValue) return 0;
        var sets = quantity / (BuyQuantity.Value + GetQuantity.Value);
        return sets * GetQuantity.Value * originalPrice;
    }

    private decimal CalculateVolumeDiscount(decimal originalPrice, int quantity)
    {
        var applicableTier = PricingTiers
            .Where(t => quantity >= t.MinQuantity && (!t.MaxQuantity.HasValue || quantity <= t.MaxQuantity.Value))
            .OrderByDescending(t => t.MinQuantity)
            .FirstOrDefault();

        if (applicableTier == null) return 0;

        if (applicableTier.DiscountPercentage.HasValue)
            return originalPrice * applicableTier.DiscountPercentage.Value / 100;

        return originalPrice - (applicableTier.Price ?? originalPrice);
    }
}

/// <summary>Pricing rule types</summary>
public enum PricingRuleType
{
    /// <summary>Percentage discount</summary>
    Percentage = 1,
    /// <summary>Fixed amount discount</summary>
    FixedAmount = 2,
    /// <summary>Buy X Get Y free</summary>
    BuyXGetY = 3,
    /// <summary>Volume-based discount</summary>
    VolumeDiscount = 4,
    /// <summary>Tiered pricing</summary>
    TieredPricing = 5,
    /// <summary>Bundle pricing</summary>
    Bundle = 6
}
