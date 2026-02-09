using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for placing an order on hold
/// </summary>
public sealed class HoldOrderCommandHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<HoldOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        HoldOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderOperationResult>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Processing)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot hold order in {order.Status} status"));
        }

        order.PlaceOnHold(request.Reason);
        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
