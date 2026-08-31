using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for getting product pricing
/// </summary>
public sealed class GetProductPricingQueryHandler(
    IProductRepository productRepository,
    IPricingEngineService pricingEngine)
    : IQueryHandler<GetProductPricingQuery, IReadOnlyList<ProductPricingDto>>
{
    public async Task<IReadOnlyList<ProductPricingDto>> Handle(
        GetProductPricingQuery request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(
            request.ProductId,
            cancellationToken,
            includePricing: true,
            isPublished: request.IncludeUnpublished ? null : true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(request.ProductId);
        }

        if (product.Pricing.Count == 0)
        {
            return Array.Empty<ProductPricingDto>();
        }

        return product.Pricing.Select(p =>
        {
            var isSaleActive = pricingEngine.IsSaleActive(p);
            var currentPrice = isSaleActive && p.SalePrice.HasValue ? p.SalePrice.Value : p.BasePrice;

            return new ProductPricingDto(
                p.Id,
                p.ProductId,
                p.Name,
                p.BasePrice,
                p.SalePrice,
                p.Currency,
                p.SaleStartDate,
                p.SaleEndDate,
                p.IsDefault,
                currentPrice,
                isSaleActive,
                p.GetCurrentActiveVersion()?.Id
            );
        }).ToList();
    }
}
