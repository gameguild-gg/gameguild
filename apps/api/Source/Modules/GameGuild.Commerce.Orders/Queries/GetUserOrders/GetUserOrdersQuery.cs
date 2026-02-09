using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Query to get orders for a user with optional status filtering
/// </summary>
public sealed record GetUserOrdersQuery(
    Guid UserId,
    OrderStatus? Status = null) : IQuery<IEnumerable<Order>>;
