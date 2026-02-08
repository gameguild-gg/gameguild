namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository interface for UserProduct entities (product access management)
/// </summary>
public interface IUserProductRepository
{
    /// <summary>
    /// Get user product access by ID
    /// </summary>
    Task<UserProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user product access by user and product IDs
    /// </summary>
    Task<UserProduct?> GetByUserAndProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all products a user has access to
    /// </summary>
    Task<IEnumerable<UserProduct>> GetByUserIdAsync(
        Guid userId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users who have access to a product
    /// </summary>
    Task<IEnumerable<UserProduct>> GetByProductIdAsync(
        Guid productId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has access to a product
    /// </summary>
    Task<bool> HasAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new user product access
    /// </summary>
    Task AddAsync(UserProduct userProduct, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update existing user product access
    /// </summary>
    Task UpdateAsync(UserProduct userProduct, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete user product access
    /// </summary>
    Task DeleteAsync(UserProduct userProduct, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user products with access expiring before a date
    /// </summary>
    Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(
        DateTime thresholdDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired subscription-based user products
    /// </summary>
    Task<IEnumerable<UserProduct>> GetExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all user products with active access status
    /// </summary>
    Task<IEnumerable<UserProduct>> GetAllActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to the database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
