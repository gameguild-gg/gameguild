using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for getting paginated promo codes
/// </summary>
public class GetPromoCodesQueryHandler(IPromoCodeRepository promoCodeRepository)
    : IQueryHandler<GetPromoCodesQuery, PagedResult<PromoCodeDto>>
{
    public async Task<PagedResult<PromoCodeDto>> Handle(GetPromoCodesQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (items, totalCount) = await promoCodeRepository.GetPagedAsync(
            request.IsActive,
            request.Type,
            request.ProductId,
            request.SearchTerm,
            request.Skip,
            request.Take,
            cancellationToken);

        var dtos = new List<PromoCodeDto>();

        foreach (var promoCode in items)
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

        return new PagedResult<PromoCodeDto>(
            dtos,
            totalCount,
            request.Skip,
            request.Take
        );
    }
}
