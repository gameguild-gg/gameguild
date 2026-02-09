using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to soft-delete an order
/// </summary>
public sealed record DeleteOrderCommand(
    Guid OrderId,
    string? Reason = null) : ICommand<Result>;
