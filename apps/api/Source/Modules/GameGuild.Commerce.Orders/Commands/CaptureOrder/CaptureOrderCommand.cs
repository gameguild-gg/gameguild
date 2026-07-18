using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to capture payment for an authorized order
/// </summary>
public sealed record CaptureOrderCommand(
    Guid OrderId,
    string PaymentMethodId) : ICommand<Result<OrderOperationResult>>;
