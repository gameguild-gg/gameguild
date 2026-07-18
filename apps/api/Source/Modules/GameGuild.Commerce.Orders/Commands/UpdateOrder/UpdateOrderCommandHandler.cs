using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for updating an order
/// </summary>
public sealed class UpdateOrderCommandHandler(
    IOrderRepository orderRepository,
    IActorContextAccessor actorContextAccessor)
    : ICommandHandler<UpdateOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        UpdateOrderCommand request,
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

        if (order.Status != OrderStatus.Pending)
        {
            return Result.Failure<OrderOperationResult>(Error.Failure("Orders.InvalidStatus", $"Cannot update order in {order.Status} status"));
        }

        if (request.Currency is not null && request.Currency != order.Currency)
        {
            return Result.Failure<OrderOperationResult>(
                Error.Validation("Orders.CurrencyImmutable", "Order currency is derived from authoritative line-item pricing."));
        }

        order.Touch();

        await orderRepository.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
