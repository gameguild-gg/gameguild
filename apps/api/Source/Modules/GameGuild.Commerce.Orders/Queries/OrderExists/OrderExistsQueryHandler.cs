using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for checking if an order exists
/// </summary>
public sealed class OrderExistsQueryHandler(
    IOrderRepository orderRepository)
    : IQueryHandler<OrderExistsQuery, bool>
{
    public async Task<bool> Handle(
        OrderExistsQuery request,
        CancellationToken cancellationToken)
    {
        var order = await orderRepository.GetByIdAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
        return order is not null;
    }
}
