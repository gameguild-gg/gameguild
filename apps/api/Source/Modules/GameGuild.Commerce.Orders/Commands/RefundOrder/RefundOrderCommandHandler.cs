using GameGuild.Commerce.Products;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for processing a refund for a captured order. Issue #1205 G-4: the refund
/// now flows through the Payments refund boundary (approval-aware gateway refund + refund
/// accounting) BEFORE any local order mutation, accepts <see cref="OrderStatus.Fulfilled"/>
/// orders (the documented happy path), and enforces the same actor ownership check as
/// capture/complete.
/// </summary>
public sealed class RefundOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderPaymentRefunder paymentRefunder,
    IEntitlementService entitlementService,
    IActorContextAccessor actorContextAccessor,
    IProductInventoryService? inventoryService = null)
    : ICommandHandler<RefundOrderCommand, Result<OrderOperationResult>>
{
    private const string DefaultRefundReason = "order_refund";

    public async Task<Result<OrderOperationResult>> Handle(
        RefundOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderOperationResult>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        var authorizationError = OrderActorContext.Authorize(order, actorContextAccessor);
        if (authorizationError is not null)
            return Result.Failure<OrderOperationResult>(authorizationError);

        if (order.Status is not (OrderStatus.Fulfilled or OrderStatus.Completed or OrderStatus.PartiallyRefunded))
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot refund order in {order.Status} status"));
        }

        // The order FSM only allows PartiallyRefunded → Refunded, so a second partial
        // refund cannot be represented; only the remaining balance can be refunded in full.
        var refundAmount = request.Amount ?? order.Total;
        if (order.Status == OrderStatus.PartiallyRefunded && refundAmount < order.Total)
        {
            return Result.Failure<OrderOperationResult>(Error.Validation(
                "Orders.RefundCompletionRequired",
                "An already partially refunded order can only be refunded for its full remaining total."));
        }

        if (refundAmount <= 0 || refundAmount > order.Total)
        {
            return Result.Failure<OrderOperationResult>(Error.Validation(
                "Orders.InvalidRefundAmount",
                $"The refund amount must be between 0 and the order total {order.Total}."));
        }

        // Only orders captured through Payments carry the internal payment binding the
        // gateway refund requires; legacy unbound orders cannot be refunded here.
        if (order.PaymentId is not { } paymentId)
        {
            return Result.Failure<OrderOperationResult>(Error.Validation(
                "Orders.PaymentRequired",
                "Only orders captured through Payments can be refunded through this endpoint."));
        }

        if (!OrderActorContext.TryResolve(actorContextAccessor, out var actor, out _))
        {
            return Result.Failure<OrderOperationResult>(Error.Unauthorized("Orders.Unauthenticated", "An authenticated tenant user context is required."));
        }

        var reason = string.IsNullOrWhiteSpace(request.Reason) ? DefaultRefundReason : request.Reason;

        // Money first: the gateway refund must complete before any local order mutation.
        // A refund queued behind the manual-approval threshold moves no money yet, so the
        // order stays untouched until the refund completes.
        var refundResult = await paymentRefunder.RefundAsync(
            new AuthoritativeOrderRefund(
                order.Id,
                order.TenantId!.Value,
                paymentId,
                refundAmount,
                order.Currency,
                reason,
                actor.UserId),
            cancellationToken).ConfigureAwait(false);

        if (refundResult.State == OrderRefundState.RequiresApproval)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure(
                "Orders.RefundRequiresApproval",
                $"Refund {refundResult.RefundId} requires manual approval before it can be processed."));
        }

        if (!refundResult.Success)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure(
                "Orders.RefundFailed",
                refundResult.FailureReason ?? "The payment provider did not process the refund."));
        }

        if (refundAmount >= order.Total)
        {
            foreach (var lineItem in order.LineItems)
            {
                await entitlementService.RevokeEntitlementAsync(
                    order.UserId, lineItem.ProductId, "Order refunded", cancellationToken).ConfigureAwait(false);
            }

            // Inventory restore (issue #401): a full refund returns tracked units to stock.
            if (inventoryService is not null && order.LineItems.Count > 0)
            {
                await inventoryService.RestoreAsync(
                    order.LineItems.Select(lineItem => (lineItem.ProductId, lineItem.Quantity)).ToList(),
                    cancellationToken).ConfigureAwait(false);
            }
        }

        order.ProcessRefund(refundAmount, reason);

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
