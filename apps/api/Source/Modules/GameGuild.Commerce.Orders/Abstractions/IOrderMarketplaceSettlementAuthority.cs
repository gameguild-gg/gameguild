namespace GameGuild.Commerce.Orders;

public enum OrderMarketplaceCurrencyChoice
{
    Hard = 1,
    Soft = 2,
    FixedMix = 3
}

public sealed record CompleteOrderMarketplaceSettlement(
    OrderMarketplaceCurrencyChoice CurrencyChoice,
    string IdempotencyKey);

public sealed record OrderMarketplaceSettlementRequest(
    Guid OrderId,
    OrderMarketplaceCurrencyChoice CurrencyChoice,
    string IdempotencyKey);

public sealed record OrderMarketplaceSettlementDecision(
    bool IsAccepted,
    Guid? SettlementId,
    bool IsDuplicate,
    string? ErrorCode,
    string? ErrorDescription)
{
    public static OrderMarketplaceSettlementDecision Accepted(Guid settlementId, bool isDuplicate)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(settlementId, Guid.Empty);
        return new OrderMarketplaceSettlementDecision(true, settlementId, isDuplicate, null, null);
    }

    public static OrderMarketplaceSettlementDecision Rejected(string errorCode, string errorDescription)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(errorDescription);
        return new OrderMarketplaceSettlementDecision(false, null, false, errorCode, errorDescription);
    }
}

public interface IOrderMarketplaceSettlementAuthority
{
    ValueTask<OrderMarketplaceSettlementDecision> SettleAsync(
        OrderMarketplaceSettlementRequest request,
        CancellationToken cancellationToken = default);
}

internal sealed class DenyOrderMarketplaceSettlementAuthority : IOrderMarketplaceSettlementAuthority
{
    public ValueTask<OrderMarketplaceSettlementDecision> SettleAsync(
        OrderMarketplaceSettlementRequest request,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(OrderMarketplaceSettlementDecision.Rejected(
            "Orders.EconomyMarketplaceDisabled",
            "Economy Marketplace is disabled."));
}
