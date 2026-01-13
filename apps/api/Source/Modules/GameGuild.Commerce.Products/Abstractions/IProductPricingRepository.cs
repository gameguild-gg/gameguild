namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository interface for ProductPricing entities
/// </summary>
public interface IProductPricingRepository
{
    /// <summary>
    /// Get pricing by ID
    /// </summary>
    Task<ProductPricing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all pricings for a product
    /// </summary>
    Task<IEnumerable<ProductPricing>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new pricing
    /// </summary>
    Task AddAsync(ProductPricing pricing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing pricing
    /// </summary>
    Task UpdateAsync(ProductPricing pricing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a pricing
    /// </summary>
    Task DeleteAsync(ProductPricing pricing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
