using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for setting product pricing with immutable price versioning
/// </summary>
public sealed class SetProductPricingCommandHandler(
    IProductRepository productRepository,
    IApplicationDbContext dbContext)
    : ICommandHandler<SetProductPricingCommand, ProductPricingDto>
{
    public async Task<ProductPricingDto> Handle(
        SetProductPricingCommand request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Check if product exists
        var product = await productRepository.GetByIdAsync(request.ProductId, cancellationToken, includePricing: true)
            .ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(request.ProductId);
        }

        ProductPricing pricing;
        ProductPricingVersion? version = null;

        if (request.PricingId.HasValue)
        {
            // Update existing pricing using immutable versioning
            pricing = product.Pricing.FirstOrDefault(p => p.Id == request.PricingId.Value)
                ?? throw new InvalidOperationException($"Pricing {request.PricingId} not found for product {request.ProductId}");

            pricing.Name = request.Name;
            pricing.Currency = request.Currency;
            pricing.SaleStartDate = request.SaleStartDate;
            pricing.SaleEndDate = request.SaleEndDate;

            // Use UpdatePrices method for immutable price changes with version tracking
            if (pricing.BasePrice != request.BasePrice || pricing.SalePrice != request.SalePrice)
            {
                version = pricing.UpdatePrices(
                    request.BasePrice,
                    request.SalePrice,
                    "Price update via SetProductPricing command",
                    request.UpdatedByUserId);

                // Add version to context
                await dbContext.Set<ProductPricingVersion>().AddAsync(version, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            // Create new pricing with initial version
            var (newPricing, initialVersion) = ProductPricing.CreateWithVersion(
                request.ProductId,
                request.Name,
                request.BasePrice,
                request.Currency,
                request.SalePrice,
                request.SaleStartDate,
                request.SaleEndDate,
                request.IsDefault,
                request.UpdatedByUserId,
                product.TenantId);

            pricing = newPricing;
            version = initialVersion;

            await dbContext.Set<ProductPricing>().AddAsync(pricing, cancellationToken).ConfigureAwait(false);
            await dbContext.Set<ProductPricingVersion>().AddAsync(version, cancellationToken).ConfigureAwait(false);
        }

        // If this pricing is set as default, unset other defaults
        if (request.IsDefault)
        {
            var otherDefaults = await dbContext.Set<ProductPricing>()
                .Where(p => p.ProductId == request.ProductId && p.Id != pricing.Id && p.IsDefault)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            foreach (var other in otherDefaults)
            {
                other.IsDefault = false;
            }

            pricing.IsDefault = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var isSaleActive = pricing.SalePrice.HasValue &&
                           (!pricing.SaleStartDate.HasValue || pricing.SaleStartDate <= SystemClock.UtcNow) &&
                           (!pricing.SaleEndDate.HasValue || pricing.SaleEndDate > SystemClock.UtcNow);

        var currentPrice = isSaleActive && pricing.SalePrice.HasValue
            ? pricing.SalePrice.Value
            : pricing.BasePrice;

        return new ProductPricingDto(
            pricing.Id,
            pricing.ProductId,
            pricing.Name,
            pricing.BasePrice,
            pricing.SalePrice,
            pricing.Currency,
            pricing.SaleStartDate,
            pricing.SaleEndDate,
            pricing.IsDefault,
            currentPrice,
            isSaleActive,
            version?.Id ?? pricing.GetCurrentActiveVersion()?.Id
        );
    }
}
