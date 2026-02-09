using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for validating a promo code
/// </summary>
public sealed class ValidatePromoCodeCommandHandler(IPricingEngineService pricingEngine)
    : ICommandHandler<ValidatePromoCodeCommand, PromoCodeValidationResult>
{
    public async Task<PromoCodeValidationResult> Handle(
        ValidatePromoCodeCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await pricingEngine.ValidatePromoCodeAsync(
            request.Code,
            request.OrderAmount,
            request.ProductId,
            request.UserId,
            cancellationToken).ConfigureAwait(false);
    }
}
