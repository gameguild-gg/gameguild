using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to refund a completed order
/// </summary>
public sealed record RefundOrderCommand(
    Guid OrderId,
    decimal? Amount = null,
    string Reason = "") : ICommand<Result<OrderOperationResult>>;
