using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for releasing a held order
/// </summary>
public sealed class ReleaseOrderCommandHandler(
    IOrderRepository orderRepository,
    IActorContextAccessor actorContextAccessor)
    : ICommandHandler<ReleaseOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        ReleaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
        {
            return Result.Failure<OrderOperationResult>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        }

        var authorizationError = OrderActorContext.Authorize(order, actorContextAccessor);
        if (authorizationError is not null)
            return Result.Failure<OrderOperationResult>(authorizationError);

        if (order.Status != OrderStatus.OnHold)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot release order in {order.Status} status"));
        }

        order.Release();
        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
