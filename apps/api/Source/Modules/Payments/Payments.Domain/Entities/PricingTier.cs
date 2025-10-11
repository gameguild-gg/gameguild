namespace GameGuild.Modules.Payments.Payments.Domain.Entities;

/// <summary>Pricing tier for volume-based pricing</summary>
public class PricingTier : EntityBase
{
    /// <summary>Pricing rule this tier belongs to</summary>
    [Required]
    public Guid PricingRuleId { get; set; }

    /// <summary>Minimum quantity for this tier</summary>
    [Required]
    public int MinQuantity { get; set; }

    /// <summary>Maximum quantity for this tier (null = unlimited)</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Price per unit at this tier</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    /// <summary>Discount percentage at this tier</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }

    /// <summary>Navigation property to pricing rule</summary>
    public virtual PricingRule PricingRule { get; set; } = null!;
}
