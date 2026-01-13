using GameGuild.Abstractions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Handler for setting product pricing
/// </summary>
public class SetProductPricingCommandHandler(
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

        if (request.PricingId.HasValue)
        {
            // Update existing pricing
            pricing = product.Pricing.FirstOrDefault(p => p.Id == request.PricingId.Value)
                ?? throw new InvalidOperationException($"Pricing {request.PricingId} not found for product {request.ProductId}");

            pricing.Name = request.Name;
            pricing.BasePrice = request.BasePrice;
            pricing.Currency = request.Currency;
            pricing.SalePrice = request.SalePrice;
            pricing.SaleStartDate = request.SaleStartDate;
            pricing.SaleEndDate = request.SaleEndDate;
            pricing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new pricing
            pricing = new ProductPricing
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                Name = request.Name,
                BasePrice = request.BasePrice,
                Currency = request.Currency,
                SalePrice = request.SalePrice,
                SaleStartDate = request.SaleStartDate,
                SaleEndDate = request.SaleEndDate,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await dbContext.Set<ProductPricing>().AddAsync(pricing, cancellationToken).ConfigureAwait(false);
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
                           (!pricing.SaleStartDate.HasValue || pricing.SaleStartDate <= DateTime.UtcNow) &&
                           (!pricing.SaleEndDate.HasValue || pricing.SaleEndDate > DateTime.UtcNow);

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
            isSaleActive
        );
    }
}
