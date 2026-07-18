using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to create a new order with idempotency protection
/// </summary>
public sealed record CreateOrderCommand(
    string IdempotencyKey,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<OrderOperationResult>>;
