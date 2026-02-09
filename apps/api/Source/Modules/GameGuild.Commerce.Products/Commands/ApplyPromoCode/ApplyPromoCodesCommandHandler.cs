using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for applying promo codes to an order
/// </summary>
public sealed class ApplyPromoCodesCommandHandler(IPricingEngineService pricingEngine)
    : ICommandHandler<ApplyPromoCodesCommand, PromoCodeApplicationResult>
{
    public async Task<PromoCodeApplicationResult> Handle(
        ApplyPromoCodesCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await pricingEngine.ApplyPromoCodesAsync(
            request.OrderAmount,
            request.PromoCodes,
            request.ProductId,
            request.UserId,
            cancellationToken).ConfigureAwait(false);
    }
}
