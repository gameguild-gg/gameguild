using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Query to check if an order exists
/// </summary>
public sealed record OrderExistsQuery(Guid OrderId) : IQuery<bool>;
