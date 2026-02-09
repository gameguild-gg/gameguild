using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for getting an order by ID
/// </summary>
public sealed class GetOrderQueryHandler(
    IOrderRepository orderRepository)
    : IQueryHandler<GetOrderQuery, Order?>
{
    public async Task<Order?> Handle(
        GetOrderQuery request,
        CancellationToken cancellationToken)
    {
        return await orderRepository.GetWithLineItemsAsync(request.OrderId, cancellationToken).ConfigureAwait(false);
    }
}
