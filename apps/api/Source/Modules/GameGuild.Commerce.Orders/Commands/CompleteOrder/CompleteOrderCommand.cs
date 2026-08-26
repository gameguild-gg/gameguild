using GameGuild.CQRS;

namespace GameGuild.Commerce.Orders;

/// <summary>
/// Command to complete an order (process payment, grant entitlements)
/// </summary>
public sealed record CompleteOrderCommand(
    Guid OrderId,
    Guid? PaymentId = null,
    string? PaymentProviderReference = null,
    string? PaymentMethod = null,
    CompleteOrderMarketplaceSettlement? MarketplaceSettlement = null) : ICommand<Result<OrderOperationResult>>;
