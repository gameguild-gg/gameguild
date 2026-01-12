using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Products;

/// <summary>
/// Represents a purchase order containing one or more line items
/// </summary>
[Table("orders")]
[Index(nameof(UserId))]
[Index(nameof(Status))]
[Index(nameof(IdempotencyKey), IsUnique = true)]
[Index(nameof(CreatedAt))]
[Index(nameof(TenantId))]
public class Order : EntityBase
{
    /// <summary>Default constructor</summary>
    public Order() { }

    /// <summary>Constructor for partial initialization</summary>
    public Order(object partial) : base(partial) { }

    /// <summary>User who placed the order</summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>Navigation property to user</summary>
    public virtual User User { get; set; } = null!;

    /// <summary>Idempotency key for preventing duplicate orders</summary>
    [Required]
    [MaxLength(100)]
    public string IdempotencyKey { get; set; } = string.Empty;

    /// <summary>Current order status</summary>
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    /// <summary>Subtotal before discounts and taxes</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Subtotal { get; set; }

    /// <summary>Total discount amount applied</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal DiscountTotal { get; set; }

    /// <summary>Tax amount</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal TaxAmount { get; set; }

    /// <summary>Final total charged</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal Total { get; set; }

    /// <summary>Currency code (ISO 4217)</summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";

    /// <summary>External payment provider reference</summary>
    [MaxLength(200)]
    public string? PaymentProviderReference { get; set; }

    /// <summary>Payment method used</summary>
    [MaxLength(50)]
    public string? PaymentMethod { get; set; }

    /// <summary>When payment was completed</summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>When order was refunded (if applicable)</summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>Refund amount if partially refunded</summary>
    [Column(TypeName = "decimal(10,2)")]
    public decimal? RefundAmount { get; set; }

    /// <summary>Reason for refund</summary>
    [MaxLength(500)]
    public string? RefundReason { get; set; }

    /// <summary>IP address of the purchaser</summary>
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    /// <summary>User agent string</summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>Additional metadata as JSON</summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>Order line items</summary>
    public virtual ICollection<OrderLineItem> LineItems { get; set; } = new List<OrderLineItem>();

    /// <summary>Create a new order with idempotency key</summary>
    public static Order Create(
        Guid userId,
        string idempotencyKey,
        string currency = "USD",
        Guid? tenantId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        return new Order
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IdempotencyKey = idempotencyKey,
            Currency = currency,
            TenantId = tenantId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Add a line item to the order</summary>
    public OrderLineItem AddLineItem(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity = 1,
        decimal discountAmount = 0,
        string? promoCodesApplied = null)
    {
        var lineItem = new OrderLineItem
        {
            Id = Guid.NewGuid(),
            OrderId = Id,
            ProductId = productId,
            ProductNameSnapshot = productName,
            UnitPriceSnapshot = unitPrice,
            Quantity = quantity,
            DiscountAmount = discountAmount,
            PromoCodesApplied = promoCodesApplied,
            LineTotal = (unitPrice * quantity) - discountAmount,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Set TenantId via reflection to bypass protected setter
        typeof(OrderLineItem).GetProperty(nameof(OrderLineItem.TenantId))!
            .SetValue(lineItem, TenantId);

        LineItems.Add(lineItem);
        RecalculateTotals();
        return lineItem;
    }

    /// <summary>Recalculate order totals from line items</summary>
    public void RecalculateTotals()
    {
        Subtotal = LineItems.Sum(li => li.UnitPriceSnapshot * li.Quantity);
        DiscountTotal = LineItems.Sum(li => li.DiscountAmount);
        Total = Subtotal - DiscountTotal + TaxAmount;
        Touch();
    }

    /// <summary>Mark order as paid</summary>
    public void MarkAsPaid(string? paymentProviderReference = null, string? paymentMethod = null)
    {
        Status = OrderStatus.Completed;
        PaidAt = DateTime.UtcNow;
        PaymentProviderReference = paymentProviderReference;
        PaymentMethod = paymentMethod;
        Touch();
    }

    /// <summary>Mark order as failed</summary>
    public void MarkAsFailed(string? reason = null)
    {
        Status = OrderStatus.Failed;
        Metadata = reason;
        Touch();
    }

    /// <summary>Process refund</summary>
    public void ProcessRefund(decimal amount, string reason)
    {
        RefundAmount = amount;
        RefundReason = reason;
        RefundedAt = DateTime.UtcNow;
        Status = amount >= Total ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
        Touch();
    }
}
