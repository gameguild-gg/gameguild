using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
///     Domain event raised when an order's state changes.
///     Provides audit trail for all order state transitions.
/// </summary>
public sealed class OrderStateChangedEvent(
    Guid orderId,
    Guid tenantId,
    OrderStatus previousStatus,
    OrderStatus newStatus,
    string? reason = null,
    string? externalPaymentId = null) : DomainEvent
{
    /// <summary>
    ///     The ID of the order that changed state
    /// </summary>
    public Guid OrderId { get; } = orderId;

    /// <summary>
    ///     The tenant this order belongs to
    /// </summary>
    public Guid TenantId { get; } = tenantId;

    /// <summary>
    ///     The previous order status before the transition
    /// </summary>
    public OrderStatus PreviousStatus { get; } = previousStatus;

    /// <summary>
    ///     The new order status after the transition
    /// </summary>
    public OrderStatus NewStatus { get; } = newStatus;

    /// <summary>
    ///     Optional reason for the state change (e.g., failure reason, cancellation reason, refund reason)
    /// </summary>
    public string? Reason { get; } = reason;

    /// <summary>
    ///     External payment ID from payment gateway (if applicable for this transition)
    /// </summary>
    public string? ExternalPaymentId { get; } = externalPaymentId;
}
