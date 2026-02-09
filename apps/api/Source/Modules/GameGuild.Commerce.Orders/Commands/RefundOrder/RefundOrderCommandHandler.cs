using GameGuild.Commerce.Products;
using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for processing a refund for an order
/// </summary>
public sealed class RefundOrderCommandHandler(
    IOrderRepository orderRepository,
    IEntitlementService entitlementService)
    : ICommandHandler<RefundOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        RefundOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderOperationResult>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        if (order.Status != OrderStatus.Completed && order.Status != OrderStatus.PartiallyRefunded)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot refund order in {order.Status} status"));
        }

        var refundAmount = request.Amount ?? order.Total;

        if (refundAmount >= order.Total)
        {
            foreach (var lineItem in order.LineItems)
            {
                await entitlementService.RevokeEntitlementAsync(
                    order.UserId, lineItem.ProductId, "Order refunded", cancellationToken).ConfigureAwait(false);
            }
        }

        order.ProcessRefund(refundAmount, request.Reason);

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
