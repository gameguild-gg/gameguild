using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
///     Immutable audit log entry for order state transitions.
///     Each state change creates a new entry for complete audit trail.
/// </summary>
[Table("order_audit_logs")]
[Index(nameof(OrderId))]
[Index(nameof(TenantId))]
[Index(nameof(OccurredAt))]
[Index(nameof(NewStatus))]
public class OrderAuditLog : EntityBase
{
    /// <summary>The order this audit entry belongs to</summary>
    [Required]
    public Guid OrderId { get; init; }

    /// <summary>Navigation property to the order</summary>
    public virtual Order Order { get; init; } = null!;

    /// <summary>The status before the transition</summary>
    [Required]
    public OrderStatus PreviousStatus { get; init; }

    /// <summary>The status after the transition</summary>
    [Required]
    public OrderStatus NewStatus { get; init; }

    /// <summary>Optional reason for the state change (failure, cancellation, refund reason)</summary>
    [MaxLength(1000)]
    public string? Reason { get; init; }

    /// <summary>External payment ID from payment gateway (for payment-related transitions)</summary>
    [MaxLength(200)]
    public string? ExternalPaymentId { get; init; }

    /// <summary>When the state transition occurred</summary>
    [Required]
    public DateTime OccurredAt { get; init; }

    /// <summary>User or system that initiated the transition (for traceability)</summary>
    [MaxLength(100)]
    public string? InitiatedBy { get; init; }

    /// <summary>IP address of the initiator (if available)</summary>
    [MaxLength(45)]
    public string? IpAddress { get; init; }

    /// <summary>Additional context as JSON (e.g., webhook data, API request details)</summary>
    [Column(TypeName = "jsonb")]
    public string? AdditionalContext { get; init; }

    /// <summary>
    ///     Creates an audit log entry from an order state changed event.
    ///     Entries are immutable once created - no setters available.
    /// </summary>
    public static OrderAuditLog FromEvent(
        OrderStateChangedEvent evt,
        string? initiatedBy = null,
        string? ipAddress = null,
        string? additionalContext = null)
    {
        return new OrderAuditLog
        {
            Id = Guid.NewGuid(),
            OrderId = evt.OrderId,
            TenantId = evt.TenantId,
            PreviousStatus = evt.PreviousStatus,
            NewStatus = evt.NewStatus,
            Reason = evt.Reason,
            ExternalPaymentId = evt.ExternalPaymentId,
            OccurredAt = evt.OccurredAt.UtcDateTime,
            InitiatedBy = initiatedBy ?? "System",
            IpAddress = ipAddress,
            AdditionalContext = additionalContext
        };
    }
}
