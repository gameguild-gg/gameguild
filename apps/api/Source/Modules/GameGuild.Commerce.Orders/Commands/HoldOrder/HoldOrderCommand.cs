using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to place an order on hold
/// </summary>
public sealed record HoldOrderCommand(
    Guid OrderId,
    string? Reason = null) : ICommand<Result<OrderOperationResult>>;
