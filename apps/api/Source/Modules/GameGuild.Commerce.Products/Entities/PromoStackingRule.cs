using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Entity representing promo code stacking rules
/// </summary>
[Table("promo_stacking_rules")]
[Index(nameof(Name), IsUnique = true)]
[Index(nameof(IsActive))]
[Index(nameof(Priority))]
public class PromoStackingRule : EntityBase
{
    /// <summary>Default constructor</summary>
    public PromoStackingRule() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial stacking rule data</param>
    public PromoStackingRule(object partial) : base(partial) { }

    /// <summary>Name of the stacking rule</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the rule</summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>Whether this rule is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Priority level (higher = applied first)</summary>
    public int Priority { get; set; } = 0;

    /// <summary>Maximum number of codes that can be stacked</summary>
    public int MaxStackableCount { get; set; } = 3;

    /// <summary>Whether exclusive codes can be stacked with others</summary>
    public bool AllowExclusiveStacking { get; set; } = false;

    /// <summary>Maximum total discount percentage allowed</summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal? MaxTotalDiscountPercentage { get; set; }

    /// <summary>Maximum total discount amount allowed</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MaxTotalDiscountAmount { get; set; }

    /// <summary>Promo code types that can be stacked together (JSON array)</summary>
    [MaxLength(500)]
    public string? AllowedTypesCombinations { get; set; }

    /// <summary>Conflict resolution strategy</summary>
    public ConflictResolutionStrategy ConflictStrategy { get; set; } = ConflictResolutionStrategy.HighestDiscount;

    /// <summary>Whether to allow same-type stacking</summary>
    public bool AllowSameTypeStacking { get; set; } = false;

    /// <summary>Minimum order amount required for stacking</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinOrderAmountForStacking { get; set; }

    /// <summary>Check if two promo codes can be stacked together</summary>
    public bool CanStack(PromoCode code1, PromoCode code2)
    {
        if (!IsActive) return false;

        // Check exclusive codes
        if (!AllowExclusiveStacking && (code1.GetIsExclusive() || code2.GetIsExclusive()))
            return false;

        // Check same type
        if (!AllowSameTypeStacking && code1.Type == code2.Type)
            return false;

        return true;
    }
}

/// <summary>Conflict resolution strategies for stacked promo codes</summary>
public enum ConflictResolutionStrategy
{
    /// <summary>Apply highest discount only</summary>
    HighestDiscount = 0,

    /// <summary>Apply lowest discount only</summary>
    LowestDiscount = 1,

    /// <summary>Apply first code only</summary>
    FirstCodeOnly = 2,

    /// <summary>Apply last code only</summary>
    LastCodeOnly = 3,

    /// <summary>Apply all codes sequentially</summary>
    Sequential = 4,

    /// <summary>Apply all codes additively</summary>
    Additive = 5
}
