using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Commerce.Orders;

public sealed record PrepareOrderPaymentIntentCommand(Guid OrderId) : ICommand<Result<OrderPaymentIntentPreparation>>;

public sealed class PrepareOrderPaymentIntentCommandHandler(
    IOrderRepository orders,
    IOrderPaymentIntentPreparer preparer,
    IActorContextAccessor actorContextAccessor)
    : ICommandHandler<PrepareOrderPaymentIntentCommand, Result<OrderPaymentIntentPreparation>>
{
    public async Task<Result<OrderPaymentIntentPreparation>> Handle(
        PrepareOrderPaymentIntentCommand request,
        CancellationToken cancellationToken)
    {
        var order = await orders.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        if (order is null)
            return Result.Failure<OrderPaymentIntentPreparation>(Error.NotFound("Orders.NotFound", $"Order {request.OrderId} not found"));
        var authorizationError = OrderActorContext.Authorize(order, actorContextAccessor);
        if (authorizationError is not null)
            return Result.Failure<OrderPaymentIntentPreparation>(authorizationError);
        if (order.Status is not (OrderStatus.Pending or OrderStatus.Processing))
            return Result.Failure<OrderPaymentIntentPreparation>(Error.Conflict("Orders.InvalidStatus", $"Cannot prepare payment in {order.Status} status."));
        if (order.Total <= 0 || order.LineItems.Count == 0 || order.TenantId is null)
            return Result.Failure<OrderPaymentIntentPreparation>(Error.Validation("Orders.InvalidPayableOrder", "The order is not payable."));

        if (order.Status == OrderStatus.Pending)
        {
            order.StartPaymentProcessing();
            await orders.UpdateAsync(order, cancellationToken).ConfigureAwait(false);
            await orders.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = await preparer.PrepareAsync(new AuthoritativeOrderPaymentIntent(
            order.Id,
            order.TenantId.Value,
            order.Total,
            order.Currency), cancellationToken).ConfigureAwait(false);
        return result.Success
            ? Result.Success(result)
            : Result.Failure<OrderPaymentIntentPreparation>(Error.Failure("Orders.PaymentIntentUnavailable", result.FailureReason ?? "PaymentIntent unavailable."));
    }
}
