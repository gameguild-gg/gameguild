using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for cancelling a pending order
/// </summary>
public sealed class CancelOrderCommandHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        if (order.Status != OrderStatus.Pending)
        {
            return Result.Failure(Error.Failure("Orders.InvalidStatus", "Cannot cancel order in current state"));
        }

        order.Cancel(request.Reason);
        order.Touch();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
