using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to cancel a pending order
/// </summary>
public sealed record CancelOrderCommand(
    Guid OrderId,
    string? Reason = null) : ICommand<Result>;
