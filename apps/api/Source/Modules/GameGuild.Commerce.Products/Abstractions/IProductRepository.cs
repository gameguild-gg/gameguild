namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository interface for Product entities
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Get a product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="includePricing">Include pricing information</param>
    /// <param name="includeCreator">Include creator information</param>
    /// <returns>Product or null if not found</returns>
    Task<Product?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default,
        bool includePricing = false,
        bool includeCreator = false);

    /// <summary>
    /// Get a paginated list of products
    /// </summary>
    Task<(IEnumerable<Product> Items, int TotalCount)> GetPagedAsync(
        ProductType? type = null,
        Guid? creatorId = null,
        string? searchTerm = null,
        bool? isBundle = null,
        int skip = 0,
        int take = 50,
        string sortBy = "CreatedAt",
        string sortDirection = "DESC",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new product
    /// </summary>
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing product
    /// </summary>
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a product
    /// </summary>
    Task DeleteAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a product exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
