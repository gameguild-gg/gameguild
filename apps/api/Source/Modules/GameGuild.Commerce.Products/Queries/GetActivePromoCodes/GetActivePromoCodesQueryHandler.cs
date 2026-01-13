using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for getting active promo codes
/// </summary>
public class GetActivePromoCodesQueryHandler(IPromoCodeRepository promoCodeRepository)
    : IQueryHandler<GetActivePromoCodesQuery, IReadOnlyList<PromoCodeDto>>
{
    public async Task<IReadOnlyList<PromoCodeDto>> Handle(
        GetActivePromoCodesQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var promoCodes = request.ProductId.HasValue
            ? await promoCodeRepository.GetByProductIdAsync(request.ProductId.Value, cancellationToken)
                .ConfigureAwait(false)
            : await promoCodeRepository.GetActiveCodesAsync(cancellationToken)
                .ConfigureAwait(false);

        var dtos = new List<PromoCodeDto>();

        foreach (var promoCode in promoCodes)
        {
            var usageCount = await promoCodeRepository.GetUsageCountAsync(promoCode.Id, cancellationToken)
                .ConfigureAwait(false);

            dtos.Add(new PromoCodeDto(
                promoCode.Id,
                promoCode.Code,
                promoCode.Name,
                promoCode.Description,
                promoCode.Type,
                promoCode.DiscountPercentage,
                promoCode.DiscountAmount,
                promoCode.Currency,
                promoCode.MinimumOrderAmount,
                promoCode.MaxUses,
                promoCode.MaxUsesPerUser,
                promoCode.ValidFrom,
                promoCode.ValidUntil,
                promoCode.IsActive,
                promoCode.IsExclusive,
                promoCode.StackingPriority,
                promoCode.ProductId,
                usageCount,
                promoCode.CreatedAt,
                promoCode.UpdatedAt
            ));
        }

        return dtos;
    }
}
