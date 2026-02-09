using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Query to get all orders (admin) with optional status filtering
/// </summary>
public sealed record GetAllOrdersQuery(
    OrderStatus? Status = null) : IQuery<IEnumerable<Order>>;
