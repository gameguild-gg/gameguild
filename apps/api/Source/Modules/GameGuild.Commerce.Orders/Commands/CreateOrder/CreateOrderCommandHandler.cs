using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for creating a new order with idempotency protection
/// </summary>
public sealed class CreateOrderCommandHandler(
    IOrderRepository orderRepository)
    : ICommandHandler<CreateOrderCommand, Result<OrderOperationResult>>
{
    public async Task<Result<OrderOperationResult>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        // Check for existing order with same idempotency key
        var existingOrder = await orderRepository.GetByIdempotencyKeyAsync(
            request.IdempotencyKey, cancellationToken).ConfigureAwait(false);

        if (existingOrder != null)
        {
            return Result.Success(OrderOperationResult.FromOrder(existingOrder, wasDuplicate: true));
        }

        // Create new order
        var order = Order.Create(
            request.UserId,
            request.IdempotencyKey,
            request.TenantId ?? Guid.Empty,
            request.Currency,
            request.IpAddress,
            request.UserAgent);

        await orderRepository.AddAsync(order, cancellationToken).ConfigureAwait(false);
        await orderRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(OrderOperationResult.FromOrder(order));
    }
}
