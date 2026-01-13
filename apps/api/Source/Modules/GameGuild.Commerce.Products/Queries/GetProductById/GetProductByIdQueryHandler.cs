using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query handler for getting a product by ID
/// </summary>
public class GetProductByIdQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductByIdQuery, ProductDto?>
{
    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var product = await productRepository.GetByIdAsync(
            request.ProductId,
            cancellationToken,
            includePricing: request.IncludePricing,
            includeCreator: request.IncludeCreator
        ).ConfigureAwait(false);

        if (product == null)
            return null;

        return new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.ShortDescription,
            product.ImageUrl,
            product.Type,
            product.IsBundle,
            product.CreatorId,
            product.GetBundleItemIds(),
            product.ReferralCommissionPercentage,
            product.MaxAffiliateDiscount,
            product.AffiliateCommissionPercentage,
            product.CreatedAt,
            product.UpdatedAt,
            request.IncludePricing ? product.Pricing.Select(p => new ProductPricingDto(
                p.Id,
                p.ProductId,
                p.Name,
                p.BasePrice,
                p.SalePrice,
                p.Currency,
                p.SaleStartDate,
                p.SaleEndDate,
                p.IsDefault,
                p.GetCurrentPrice(),
                p.IsSaleActive()
            )).ToList() : null
        );
    }
}
