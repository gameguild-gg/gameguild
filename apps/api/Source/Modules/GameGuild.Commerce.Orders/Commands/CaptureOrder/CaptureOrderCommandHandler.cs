using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for capturing payment for an authorized order
/// </summary>
public sealed class CaptureOrderCommandHandler(
    IOrderRepository orderRepository,
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

        return Result.Failure<OrderOperationResult>(
            Error.Forbidden(
                "Orders.PaymentAuthorityRequired",
                "Order capture is disabled until Payments supplies an authoritative order binding."));
    }
}
