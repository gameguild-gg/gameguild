using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Represents a single line item in an order with price snapshot
/// </summary>
[Table("order_line_items")]
[Index(nameof(OrderId))]
[Index(nameof(ProductId))]
public class OrderLineItem : EntityBase
{
    /// <summary>Default constructor</summary>
    public OrderLineItem() { }

    /// <summary>Constructor for partial initialization</summary>
    public OrderLineItem(object partial) : base(partial) { }

    /// <summary>Foreign key to order</summary>
    [Required]
    public Guid OrderId { get; set; }

    /// <summary>Navigation property to order</summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>Foreign key to product</summary>
    [Required]
    public Guid ProductId { get; set; }

    /// <summary>Navigation property to product</summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>Product name at time of purchase (snapshot)</summary>
    [Required]
    [MaxLength(200)]
    public string ProductNameSnapshot { get; set; } = string.Empty;

    /// <summary>Unit price at time of purchase (snapshot)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPriceSnapshot { get; set; }

    /// <summary>Original base price before any discounts</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePriceSnapshot { get; set; }

    /// <summary>Sale price if on sale (snapshot)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePriceSnapshot { get; set; }

    /// <summary>Quantity purchased</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Total discount applied to this line item</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; set; }

    /// <summary>JSON array of promo codes applied</summary>
    [MaxLength(500)]
    public string? PromoCodesApplied { get; set; }

    /// <summary>Calculated line total (unit price * quantity - discount)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal { get; set; }

    /// <summary>ID of the pricing tier used</summary>
    public Guid? PricingTierId { get; set; }

    /// <summary>Name of the pricing tier used (snapshot)</summary>
    [MaxLength(100)]
    public string? PricingTierNameSnapshot { get; set; }

    /// <summary>Whether this was a subscription product</summary>
    public bool IsSubscription { get; set; }

    /// <summary>Subscription plan ID if subscription</summary>
    public Guid? SubscriptionPlanId { get; set; }

    /// <summary>Subscription billing interval (snapshot)</summary>
    [MaxLength(20)]
    public string? BillingIntervalSnapshot { get; set; }

    /// <summary>ID of the UserProduct record created</summary>
    public Guid? UserProductId { get; set; }

    /// <summary>Navigation to UserProduct</summary>
    public virtual UserProduct? UserProduct { get; set; }
}
