using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to create a new order with idempotency protection
/// </summary>
public sealed record CreateOrderCommand(
    Guid UserId,
    string IdempotencyKey,
    string Currency = "USD",
    Guid? TenantId = null,
    string? IpAddress = null,
    string? UserAgent = null) : ICommand<Result<OrderOperationResult>>;
