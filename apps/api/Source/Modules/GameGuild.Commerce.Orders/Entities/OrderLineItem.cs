using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Commerce.Products;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Represents a single line item in an order with price snapshot
/// </summary>
[Table("order_line_items")]
[Index(nameof(OrderId))]
[Index(nameof(ProductId))]
public class OrderLineItem : EntityBase
{
    private OrderLineItem() { }

    /// <summary>Foreign key to order</summary>
    [Required]
    public Guid OrderId { get; private set; }

    /// <summary>Navigation property to order</summary>
    public virtual Order Order { get; set; } = null!;

    /// <summary>Foreign key to product</summary>
    [Required]
    public Guid ProductId { get; private set; }

    /// <summary>Navigation property to product</summary>
    public virtual Product Product { get; set; } = null!;

    /// <summary>Product name at time of purchase (snapshot)</summary>
    [Required]
    [MaxLength(200)]
    public string ProductNameSnapshot { get; private set; } = string.Empty;

    /// <summary>Unit price at time of purchase (snapshot)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal UnitPriceSnapshot { get; private set; }

    /// <summary>Original base price before any discounts</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal BasePriceSnapshot { get; private set; }

    /// <summary>Sale price if on sale (snapshot)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? SalePriceSnapshot { get; private set; }

    /// <summary>Currency resolved from the immutable pricing version</summary>
    [Required]
    [MaxLength(3)]
    public string CurrencySnapshot { get; private set; } = string.Empty;

    /// <summary>Quantity purchased</summary>
    public int Quantity { get; private set; } = 1;

    /// <summary>Total discount applied to this line item</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountAmount { get; private set; }

    /// <summary>JSON array of promo codes applied</summary>
    [MaxLength(500)]
    public string? PromoCodesApplied { get; private set; }

    /// <summary>Calculated line total (unit price * quantity - discount)</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal LineTotal { get; private set; }

    /// <summary>ID of the explicitly selected product pricing tier</summary>
    public Guid ProductPricingId { get; private set; }

    /// <summary>ID of the immutable product pricing version used</summary>
    public Guid ProductPricingVersionId { get; private set; }

    /// <summary>Sequential product price version captured at checkout</summary>
    public int PriceVersionSnapshot { get; private set; }

    /// <summary>Name of the pricing tier used (snapshot)</summary>
    [MaxLength(100)]
    public string? PricingTierNameSnapshot { get; private set; }

    /// <summary>Whether this was a subscription product</summary>
    public bool IsSubscription { get; private set; }

    /// <summary>Subscription plan ID if subscription</summary>
    public Guid? SubscriptionPlanId { get; private set; }

    /// <summary>Subscription billing interval (snapshot)</summary>
    [MaxLength(20)]
    public string? BillingIntervalSnapshot { get; private set; }

    /// <summary>ID of the UserProduct record created</summary>
    public Guid? UserProductId { get; private set; }

    /// <summary>Navigation to UserProduct</summary>
    public virtual UserProduct? UserProduct { get; set; }

    internal static OrderLineItem Create(
        Guid orderId,
        Guid tenantId,
        Guid productId,
        string productName,
        string pricingTierName,
        OrderLineItemPricingSnapshot pricing,
        int quantity,
        decimal discountAmount,
        string? promoCodesApplied,
        bool isSubscription)
    {
        return new OrderLineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            TenantId = tenantId,
            ProductId = productId,
            ProductNameSnapshot = productName,
            ProductPricingId = pricing.ProductPricingId,
            ProductPricingVersionId = pricing.ProductPricingVersionId,
            PriceVersionSnapshot = pricing.PriceVersion,
            UnitPriceSnapshot = pricing.UnitPrice,
            BasePriceSnapshot = pricing.BasePrice,
            SalePriceSnapshot = pricing.SalePrice,
            CurrencySnapshot = pricing.Currency,
            PricingTierNameSnapshot = pricingTierName,
            Quantity = quantity,
            DiscountAmount = discountAmount,
            PromoCodesApplied = promoCodesApplied,
            LineTotal = (pricing.UnitPrice * quantity) - discountAmount,
            IsSubscription = isSubscription
        };
    }

    internal void AttachEntitlement(Guid userProductId)
    {
        UserProductId = userProductId;
        Touch();
    }

    public override void SetProperties(Dictionary<string, object?> properties)
    {
        throw new InvalidOperationException("Order line-item snapshots cannot be changed after creation.");
    }
}
