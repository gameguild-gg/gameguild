using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command handler for creating a new product
/// </summary>
public class CreateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<CreateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Create new product entity
        var product = Product.Create(
            request.Name,
            request.Type,
            request.Description,
            request.ShortDescription,
            request.ImageUrl,
            request.CreatorId,
            request.IsBundle,
            request.ReferralCommissionPercentage,
            request.MaxAffiliateDiscount,
            request.AffiliateCommissionPercentage,
            request.TenantId
        );

        // Set bundle items if applicable
        if (request.IsBundle && request.BundleItems?.Count > 0)
        {
            product.SetBundleItemIds(request.BundleItems);
        }

        // Add to repository
        await productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        await productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Map to DTO
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
            product.UpdatedAt
        );
    }
}
