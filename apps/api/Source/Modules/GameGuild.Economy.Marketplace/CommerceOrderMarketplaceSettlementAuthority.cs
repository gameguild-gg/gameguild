using GameGuild.Commerce.Orders;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Risk;

namespace GameGuild.Economy.Marketplace;

public sealed class CommerceOrderMarketplaceSettlementAuthority(
    IDurableMarketplaceSettlementService settlements,
    TimeProvider timeProvider) : IOrderMarketplaceSettlementAuthority
{
    public async ValueTask<OrderMarketplaceSettlementDecision> SettleAsync(
        OrderMarketplaceSettlementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await settlements.SettleAsync(
            new SettleAuthoritativeMarketplaceOrderRequest(
                request.TenantId,
                request.ActorId,
                request.OrderId,
                Map(request.CurrencyChoice),
                EconomySubjectReference.ForUser(request.TenantId, request.ActorId),
                request.JurisdictionCode,
                request.RiskDecisionId,
                request.OperationFingerprint,
                new IdempotencyKey(request.IdempotencyKey),
                timeProvider.GetUtcNow()),
            cancellationToken).ConfigureAwait(false);
        return OrderMarketplaceSettlementDecision.Accepted(
            result.SettlementId,
            result.IsDuplicate);
    }

    private static MarketplaceCurrencyChoice Map(OrderMarketplaceCurrencyChoice choice) => choice switch
    {
        OrderMarketplaceCurrencyChoice.Hard => MarketplaceCurrencyChoice.Hard,
        OrderMarketplaceCurrencyChoice.Soft => MarketplaceCurrencyChoice.Soft,
        OrderMarketplaceCurrencyChoice.FixedMix => MarketplaceCurrencyChoice.FixedMix,
        _ => throw new ArgumentOutOfRangeException(nameof(choice))
    };
}
