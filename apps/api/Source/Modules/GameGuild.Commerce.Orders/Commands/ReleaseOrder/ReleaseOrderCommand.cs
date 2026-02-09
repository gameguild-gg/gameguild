using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to release a held order
/// </summary>
public sealed record ReleaseOrderCommand(
    Guid OrderId) : ICommand<Result<OrderOperationResult>>;
