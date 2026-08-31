using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Query handler for getting a paginated list of products
/// </summary>
public sealed class GetProductsPagedQueryHandler(IProductRepository productRepository)
    : IQueryHandler<GetProductsPagedQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsPagedQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (products, totalCount) = await productRepository.GetPagedAsync(
            type: request.Type,
            creatorId: request.CreatorId,
            searchTerm: request.SearchTerm,
            isBundle: request.IsBundle,
            isPublished: request.IncludeUnpublished ? null : true,
            skip: request.Skip,
            take: request.Take,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            tenantId: request.TenantId,
            cancellationToken: cancellationToken
        ).ConfigureAwait(false);

        var dtos = products.Select(product => new ProductDto(
            product.Id,
            product.Name,
            product.Description,
            product.ShortDescription,
            product.ImageUrl,
            product.Type,
            product.IsBundle,
            product.IsPublished,
            product.CreatorId,
            product.GetBundleProductIds().ToList(),
            product.CommissionConfig?.ReferralCommissionPercentage ?? 0m,
            product.CommissionConfig?.MaxAffiliateDiscount ?? 0m,
            product.CommissionConfig?.AffiliateCommissionPercentage ?? 30m,
            product.CreatedAt,
            product.UpdatedAt,
            TenantId: product.TenantId
        )).ToList();

        return new PagedResult<ProductDto>(
            dtos,
            totalCount,
            request.Skip,
            request.Take
        );
    }
}
