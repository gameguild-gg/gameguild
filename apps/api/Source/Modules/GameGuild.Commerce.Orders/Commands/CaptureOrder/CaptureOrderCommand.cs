using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to capture payment for an authorized order
/// </summary>
public sealed record CaptureOrderCommand(
    Guid OrderId,
    decimal? Amount = null) : ICommand<Result<OrderOperationResult>>;
