namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository interface for PromoCode entities
/// </summary>
public interface IPromoCodeRepository
{
    /// <summary>
    /// Get a promo code by ID
    /// </summary>
    Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a promo code by code string
    /// </summary>
    Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active promo codes
    /// </summary>
    Task<IEnumerable<PromoCode>> GetActiveCodesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all promo codes for a specific product
    /// </summary>
    Task<IEnumerable<PromoCode>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a paginated list of promo codes
    /// </summary>
    Task<(IEnumerable<PromoCode> Items, int TotalCount)> GetPagedAsync(
        bool? isActive = null,
        PromoCodeType? type = null,
        Guid? productId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get usage count for a promo code
    /// </summary>
    Task<int> GetUsageCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get usage count for a promo code by a specific user
    /// </summary>
    Task<int> GetUserUsageCountAsync(Guid promoCodeId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new promo code
    /// </summary>
    Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing promo code
    /// </summary>
    Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a promo code
    /// </summary>
    Task DeleteAsync(PromoCode promoCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a promo code usage
    /// </summary>
    Task RecordUsageAsync(PromoCodeUse usage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a promo code exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a promo code string is already in use
    /// </summary>
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
