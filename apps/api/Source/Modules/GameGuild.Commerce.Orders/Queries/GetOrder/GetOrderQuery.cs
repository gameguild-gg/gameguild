using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Query to get an order by ID
/// </summary>
public sealed record GetOrderQuery(Guid OrderId) : IQuery<Order?>;
