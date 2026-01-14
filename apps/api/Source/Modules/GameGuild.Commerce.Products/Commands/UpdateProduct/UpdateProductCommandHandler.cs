using GameGuild.CQRS;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Command handler for updating an existing product
/// </summary>
#pragma warning disable CS0618 // Type or member is obsolete - Deprecated properties intentionally used during migration period
public class UpdateProductCommandHandler(IProductRepository productRepository)
    : ICommandHandler<UpdateProductCommand, ProductDto>
{
    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Get existing product
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken).ConfigureAwait(false)
            ?? throw new ProductNotFoundException(request.ProductId);

        // Check version for optimistic concurrency
        if (request.ExpectedVersion.HasValue && product.Version != request.ExpectedVersion.Value)
        {
            throw new ConcurrencyException($"Product {request.ProductId} has been modified by another user.");
        }

        // Update properties
        if (!string.IsNullOrWhiteSpace(request.Name))
            product.Name = request.Name;

        if (request.Description != null)
            product.Description = request.Description;

        if (request.ShortDescription != null)
            product.ShortDescription = request.ShortDescription;

        if (request.ImageUrl != null)
            product.ImageUrl = request.ImageUrl;

        if (request.Type.HasValue)
            product.Type = request.Type.Value;

        if (request.IsBundle.HasValue)
            product.IsBundle = request.IsBundle.Value;

        if (request.BundleItems != null)
            product.SetBundleItemIds(request.BundleItems);

        if (request.ReferralCommissionPercentage.HasValue)
            product.ReferralCommissionPercentage = request.ReferralCommissionPercentage.Value;

        if (request.MaxAffiliateDiscount.HasValue)
            product.MaxAffiliateDiscount = request.MaxAffiliateDiscount.Value;

        if (request.AffiliateCommissionPercentage.HasValue)
            product.AffiliateCommissionPercentage = request.AffiliateCommissionPercentage.Value;

        // Touch for audit trail
        product.Touch();

        // Save changes
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
#pragma warning restore CS0618
