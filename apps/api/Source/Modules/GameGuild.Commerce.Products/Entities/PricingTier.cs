using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Entity representing pricing tiers for products
/// </summary>
[Table("pricing_tiers")]
[Index(nameof(ProductId))]
[Index(nameof(MinQuantity))]
[Index(nameof(IsActive))]
public class PricingTier : EntityBase
{
    /// <summary>Default constructor</summary>
    public PricingTier() { }

    /// <summary>Constructor for partial initialization</summary>
    /// <param name="partial">Partial pricing tier data</param>
    public PricingTier(object partial) : base(partial) { }

    /// <summary>Foreign key to the Product entity</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to the Product entity</summary>
    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    /// <summary>Name of the tier (e.g., "Starter", "Business", "Enterprise")</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Description of the tier</summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>Minimum quantity to qualify for this tier</summary>
    public int MinQuantity { get; set; } = 1;

    /// <summary>Maximum quantity for this tier (null = unlimited)</summary>
    public int? MaxQuantity { get; set; }

    /// <summary>Price per unit at this tier</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPrice { get; set; }

    /// <summary>Currency code</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>Whether this tier is active</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Display order for UI</summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>Check if the tier applies to the given quantity</summary>
    public bool AppliesToQuantity(int quantity)
    {
        if (!IsActive) return false;

        return quantity >= MinQuantity && (MaxQuantity == null || quantity <= MaxQuantity);
    }

    /// <summary>Calculate total price for the given quantity</summary>
    public decimal CalculateTotalPrice(int quantity)
    {
        if (!AppliesToQuantity(quantity)) return 0;

        return UnitPrice * quantity;
    }
}
