using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Handler for getting orders for a user
/// </summary>
public sealed class GetUserOrdersQueryHandler(
    IOrderRepository orderRepository)
    : IQueryHandler<GetUserOrdersQuery, IEnumerable<Order>>
{
    public async Task<IEnumerable<Order>> Handle(
        GetUserOrdersQuery request,
        CancellationToken cancellationToken)
    {
        return await orderRepository.GetByUserIdAsync(request.UserId, request.Status, cancellationToken).ConfigureAwait(false);
    }
}
