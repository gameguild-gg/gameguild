using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to partially update an order
/// </summary>
public sealed record UpdateOrderCommand(
    Guid OrderId,
    string? Currency = null,
    string? Notes = null,
    Dictionary<string, string>? Metadata = null) : ICommand<Result<OrderOperationResult>>;
