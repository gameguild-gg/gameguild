namespace GameGuild.Commerce.Products;

/// <summary>
/// Service for managing product pricing
/// </summary>
public class ProductPricingService(
    IProductRepository productRepository,
    IProductPricingRepository pricingRepository,
    IPricingEngineService pricingEngine) : IProductPricingService
{
    /// <inheritdoc />
    public async Task<ProductPricing> CreatePricingAsync(ProductPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        var product = await productRepository.GetByIdAsync(
            pricing.ProductId,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(pricing.ProductId);
        }

        // If this is marked as default, unset other defaults
        if (pricing.IsDefault && product.Pricing.Count > 0)
        {
            foreach (var existingPricing in product.Pricing.Where(p => p.IsDefault))
            {
                existingPricing.IsDefault = false;
            }
        }

        product.Pricing.Add(pricing);

        await productRepository.UpdateAsync(product).ConfigureAwait(false);
        await productRepository.SaveChangesAsync().ConfigureAwait(false);

        return pricing;
    }

    /// <inheritdoc />
    public async Task<ProductPricing?> GetPricingByIdAsync(Guid id)
    {
        return await pricingRepository.GetByIdAsync(id).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProductPricing>> GetPricingsByProductIdAsync(Guid productId)
    {
        var product = await productRepository.GetByIdAsync(
            productId,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        return product.Pricing;
    }

    /// <inheritdoc />
    public async Task<ProductPricing?> GetDefaultPricingForProductAsync(Guid productId)
    {
        var product = await productRepository.GetByIdAsync(
            productId,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(productId);
        }

        return product.Pricing.FirstOrDefault(p => p.IsDefault)
               ?? product.Pricing.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<ProductPricing> UpdatePricingAsync(ProductPricing pricing)
    {
        ArgumentNullException.ThrowIfNull(pricing);

        var product = await productRepository.GetByIdAsync(
            pricing.ProductId,
            includePricing: true).ConfigureAwait(false);

        if (product == null)
        {
            throw new ProductNotFoundException(pricing.ProductId);
        }

        var existingPricing = product.Pricing.FirstOrDefault(p => p.Id == pricing.Id);
        if (existingPricing == null)
        {
            throw new InvalidOperationException($"Pricing {pricing.Id} not found for product {pricing.ProductId}");
        }

        // Update properties
        existingPricing.Name = pricing.Name;
        existingPricing.BasePrice = pricing.BasePrice;
        existingPricing.SalePrice = pricing.SalePrice;
        existingPricing.Currency = pricing.Currency;
        existingPricing.SaleStartDate = pricing.SaleStartDate;
        existingPricing.SaleEndDate = pricing.SaleEndDate;

        if (pricing.IsDefault && !existingPricing.IsDefault)
        {
            // Unset other defaults
            foreach (var otherPricing in product.Pricing.Where(p => p.IsDefault && p.Id != pricing.Id))
            {
                otherPricing.IsDefault = false;
            }
        }

        existingPricing.IsDefault = pricing.IsDefault;
        existingPricing.Touch();

        await productRepository.UpdateAsync(product).ConfigureAwait(false);
        await productRepository.SaveChangesAsync().ConfigureAwait(false);

        return existingPricing;
    }

    /// <inheritdoc />
    public async Task<bool> DeletePricingAsync(Guid id)
    {
        var pricing = await pricingRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (pricing == null)
        {
            return false;
        }

        pricing.SoftDelete();

        await pricingRepository.UpdateAsync(pricing).ConfigureAwait(false);
        await pricingRepository.SaveChangesAsync().ConfigureAwait(false);
        
        return true;
    }

    /// <inheritdoc />
    public async Task<decimal> GetEffectivePriceAsync(Guid productId, Guid? promoCodeId = null)
    {
        return await pricingEngine.GetCurrentPriceAsync(productId).ConfigureAwait(false);
    }
}
