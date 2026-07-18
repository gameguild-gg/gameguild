using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for capturing payment for an authorized order
/// </summary>
public sealed class CaptureOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderPaymentProcessor paymentProcessor,
    IActorContextAccessor actorContextAccessor)
    : ICommandHandler<CaptureOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        CaptureOrderCommand request,
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

        if (order.Status == OrderStatus.Paid && order.PaymentId.HasValue)
        {
            return Result.Success(OrderOperationResult.FromOrder(order, wasDuplicate: true));
        }

        if (order.Status is not (OrderStatus.Pending or OrderStatus.Processing))
        {
            return Result.Failure<OrderOperationResult>(
                Error.Conflict("Orders.InvalidStatus", $"Cannot capture an order in {order.Status} status."));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodId))
        {
            return Result.Failure<OrderOperationResult>(
                Error.Validation("Orders.PaymentMethodRequired", "A payment method is required."));
        }

        if (order.Total <= 0 ||
            order.LineItems.Count == 0 ||
            order.LineItems.Any(lineItem =>
                lineItem.ProductPricingId == Guid.Empty ||
                lineItem.ProductPricingVersionId == Guid.Empty ||
                lineItem.PriceVersionSnapshot < 1 ||
                lineItem.UnitPriceSnapshot <= 0 ||
                lineItem.LineTotal <= 0 ||
                lineItem.CurrencySnapshot != order.Currency))
        {
            return Result.Failure<OrderOperationResult>(
                Error.Validation(
                    "Orders.InvalidPayableOrder",
                    "The order does not contain authoritative payable line-item snapshots."));
        }

        if (order.LineItems.Any(lineItem => lineItem.IsSubscription))
        {
            return Result.Failure<OrderOperationResult>(
                Error.Forbidden(
                    "Orders.SubscriptionAuthorityRequired",
                    "Subscription products require the authoritative subscription checkout."));
        }

        var paymentMethodError = paymentProcessor.GetPaymentMethodValidationError(request.PaymentMethodId);
        if (paymentMethodError is not null)
        {
            return Result.Failure<OrderOperationResult>(
                Error.Validation("Orders.InvalidPaymentMethod", paymentMethodError));
        }

        if (order.Status == OrderStatus.Pending)
        {
            order.StartPaymentProcessing();
            await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            try
            {
                await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Failure<OrderOperationResult>(
                    Error.Conflict(
                        "Orders.ConcurrentModification",
                        "The order changed before payment capture and was not charged."));
            }
        }

        OrderChargeResult chargeResult;
        try
        {
            chargeResult = await paymentProcessor.ProcessAsync(
                new AuthoritativeOrderCharge(
                    order.Id,
                    order.TenantId!.Value,
                    order.Total,
                    order.Currency,
                    request.PaymentMethodId),
                cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<OrderOperationResult>(
                Error.Conflict(
                    "Orders.PaymentConflict",
                    "The existing payment no longer matches the authoritative order snapshot."));
        }

        if (!chargeResult.Success || !chargeResult.PaymentId.HasValue)
        {
            return Result.Failure<OrderOperationResult>(
                Error.Failure(
                    "Orders.PaymentFailed",
                    chargeResult.FailureReason ?? "The payment provider did not settle the order."));
        }

        order.MarkAsPaidPendingFulfillment(chargeResult.PaymentId.Value, chargeResult.ExternalPaymentId);
        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        try
        {
            await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result.Failure<OrderOperationResult>(
                Error.Conflict(
                    "Orders.ConcurrentModification",
                    "Payment settled, but another capture already updated the order. Reload the order before retrying."));
        }

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
