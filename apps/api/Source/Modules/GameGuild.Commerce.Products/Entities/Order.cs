using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Entities;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

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

    /// <summary>Create a new order with idempotency key (TenantId required - fail-closed)</summary>
    /// <exception cref="ArgumentException">Thrown when tenantId is null or empty</exception>
    public static Order Create(
        Guid userId,
        string idempotencyKey,
        Guid tenantId,
        string currency = "USD",
        string? ipAddress = null,
        string? userAgent = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required for financial entities (fail-closed)", nameof(tenantId));

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

    /// <summary>
    ///     Valid state transitions for orders (monotonic state machine)
    /// </summary>
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        { OrderStatus.Pending, new HashSet<OrderStatus> { OrderStatus.Processing, OrderStatus.Completed, OrderStatus.Failed, OrderStatus.Cancelled } },
        { OrderStatus.Processing, new HashSet<OrderStatus> { OrderStatus.Completed, OrderStatus.Failed, OrderStatus.Cancelled } },
        { OrderStatus.Completed, new HashSet<OrderStatus> { OrderStatus.Refunded, OrderStatus.PartiallyRefunded, OrderStatus.Disputed } },
        { OrderStatus.Failed, new HashSet<OrderStatus>() }, // Terminal state
        { OrderStatus.Cancelled, new HashSet<OrderStatus>() }, // Terminal state
        { OrderStatus.Refunded, new HashSet<OrderStatus>() }, // Terminal state
        { OrderStatus.PartiallyRefunded, new HashSet<OrderStatus> { OrderStatus.Refunded, OrderStatus.Disputed } },
        { OrderStatus.Disputed, new HashSet<OrderStatus> { OrderStatus.Completed, OrderStatus.Refunded } }
    };

    /// <summary>
    ///     Validates if a state transition is allowed (monotonic enforcement)
    /// </summary>
    public bool CanTransitionTo(OrderStatus newStatus)
    {
        if (!ValidTransitions.TryGetValue(Status, out var allowed))
            return false;
        return allowed.Contains(newStatus);
    }

    /// <summary>
    ///     Transitions to a new status with validation
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when transition is not allowed</exception>
    private void TransitionTo(OrderStatus newStatus)
    {
        if (!CanTransitionTo(newStatus))
            throw new InvalidOperationException($"Invalid order state transition: {Status} -> {newStatus}");
        Status = newStatus;
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
        typeof(OrderLineItem).GetProperty(nameof(TenantId))!
            .SetValue(lineItem, TenantId);

        LineItems.Add(lineItem);
        RecalculateTotals();
        return lineItem;
    }

    /// <summary>Recalculate order totals from line items (only allowed before completion)</summary>
    /// <exception cref="InvalidOperationException">Thrown when order is already completed</exception>
    public void RecalculateTotals()
    {
        if (Status != OrderStatus.Pending && Status != OrderStatus.Processing)
            throw new InvalidOperationException($"Cannot modify order totals in {Status} status. Financial amounts are immutable after processing.");

        Subtotal = LineItems.Sum(li => li.UnitPriceSnapshot * li.Quantity);
        DiscountTotal = LineItems.Sum(li => li.DiscountAmount);
        Total = Subtotal - DiscountTotal + TaxAmount;
        Touch();
    }

    /// <summary>Mark order as paid (with state machine validation)</summary>
    public void MarkAsPaid(string? paymentProviderReference = null, string? paymentMethod = null)
    {
        TransitionTo(OrderStatus.Completed);
        PaidAt = DateTime.UtcNow;
        PaymentProviderReference = paymentProviderReference;
        PaymentMethod = paymentMethod;
        Touch();
    }

    /// <summary>Mark order as failed (with state machine validation)</summary>
    public void MarkAsFailed(string? reason = null)
    {
        TransitionTo(OrderStatus.Failed);
        Metadata = reason;
        Touch();
    }

    /// <summary>Process refund (with state machine validation)</summary>
    public void ProcessRefund(decimal amount, string reason)
    {
        var newStatus = amount >= Total ? OrderStatus.Refunded : OrderStatus.PartiallyRefunded;
        TransitionTo(newStatus);
        RefundAmount = amount;
        RefundReason = reason;
        RefundedAt = DateTime.UtcNow;
        Touch();
    }
}
