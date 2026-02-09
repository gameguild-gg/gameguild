using GameGuild.CQRS;
using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Orders;

/// <summary>
///     Handles order state change events by persisting audit log entries.
///     This provides a complete audit trail for all order state transitions.
/// </summary>
public sealed class OrderStateChangedEventHandler(
    IApplicationDbContext dbContext,
    ILogger<OrderStateChangedEventHandler> logger) : INotificationHandler<OrderStateChangedEvent>
{
    /// <summary>
    ///     Handles the order state changed event by creating an audit log entry.
    /// </summary>
    public async Task Handle(OrderStateChangedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} state changed: {PreviousStatus} -> {NewStatus} for tenant {TenantId}. Reason: {Reason}",
            notification.OrderId,
            notification.PreviousStatus,
            notification.NewStatus,
            notification.TenantId,
            notification.Reason ?? "None");

        // Create immutable audit log entry
        var auditLog = OrderAuditLog.FromEvent(
            notification,
            initiatedBy: null, // Could be enhanced to capture current user from IActorContextAccessor
            ipAddress: null,   // Could be enhanced to capture from HttpContext
            additionalContext: null);

        dbContext.Set<OrderAuditLog>().Add(auditLog);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogDebug(
            "Created audit log entry {AuditLogId} for order {OrderId} state transition",
            auditLog.Id,
            notification.OrderId);
    }
}
