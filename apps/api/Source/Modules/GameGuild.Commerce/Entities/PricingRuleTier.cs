using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce;

/// <summary>Entity representing a pricing tier for pricing rule-based volume discounts</summary>
[Table("pricing_rule_tiers")]
[Index(nameof(PricingRuleId))]
[Index(nameof(MinQuantity))]
[Index(nameof(MaxQuantity))]
public class PricingRuleTier : EntityBase
{
    /// <summary>Default constructor</summary>
    public PricingRuleTier() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial tier data</param>
    public PricingRuleTier(object partial) : base(partial) { }

    /// <summary>Foreign key to the PricingRule entity</summary>
    [Required]
    public Guid PricingRuleId { get; set; }

    /// <summary>Navigation property to the PricingRule entity</summary>
    [ForeignKey(nameof(PricingRuleId))]
    public virtual PricingRule PricingRule { get; set; } = null!;

    /// <summary>Minimum quantity for this tier</summary>
    public int? MinQuantity { get; set; }

    /// <summary>Maximum quantity for this tier</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Price for this tier</summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Price { get; set; }

    /// <summary>Discount percentage for this tier</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? DiscountPercentage { get; set; }
}
