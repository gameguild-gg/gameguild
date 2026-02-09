using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for soft-deleting an order
/// </summary>
public sealed class DeleteOrderCommandHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<DeleteOrderCommand, Result>
{
    public async Task<Result> Handle(
        DeleteOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Cancelled)
        {
            return Result.Failure(Error.Failure("Orders.InvalidStatus", "Cannot delete order in current state"));
        }

        order.SoftDelete();
        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
