using GameGuild.Commerce.Products;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for completing an order with payment and entitlement granting
/// </summary>
public sealed class CompleteOrderCommandHandler(
    IOrderRepository orderRepository,
    IEntitlementService entitlementService,
    IApplicationDbContext dbContext)
    : ICommandHandler<CompleteOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        CompleteOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderOperationResult>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        // Idempotent: already completed or fulfilled
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Fulfilled)
        {
            return Result.Success(OrderOperationResult.FromOrder(order, wasDuplicate: true));
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing && order.Status != OrderStatus.Paid)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot complete order in {order.Status} status"));
        }

        await using var transaction = await dbContext.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (order.Status != OrderStatus.Paid)
            {
                if (request.PaymentId.HasValue)
                {
                    order.MarkAsPaidPendingFulfillment(request.PaymentId.Value, request.PaymentProviderReference);
                }
                else
                {
                    order.MarkAsPaid(request.PaymentProviderReference, request.PaymentMethod, request.PaymentProviderReference);
                }
            }

            foreach (var lineItem in order.LineItems)
            {
                var acquisitionType = lineItem.IsSubscription
                    ? ProductAcquisitionType.Subscription
                    : ProductAcquisitionType.Purchase;

                var entitlementResult = await entitlementService.GrantEntitlementAsync(
                    order.UserId,
                    lineItem.ProductId,
                    acquisitionType,
                    lineItem.LineTotal,
                    order.Currency,
                    expiresAt: null,
                    orderId: order.Id,
                    cancellationToken).ConfigureAwait(false);

                if (entitlementResult.Success && entitlementResult.UserProduct != null)
                {
                    lineItem.UserProductId = entitlementResult.UserProduct.Id;
                }
            }

            order.MarkAsFulfilled();

            await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success(OrderOperationResult.FromOrder(order));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }
}
