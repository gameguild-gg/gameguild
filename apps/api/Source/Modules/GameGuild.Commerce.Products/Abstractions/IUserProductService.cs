namespace GameGuild.Commerce.Products;

/// <summary>
/// Interface for user product access services
/// </summary>
public interface IUserProductService
{
    /// <summary>
    /// Grant a user access to a product
    /// </summary>
    Task<UserProduct> GrantProductAccessAsync(
        Guid userId,
        Guid productId,
        ProductAcquisitionType acquisitionType,
        decimal pricePaid = 0,
        string currency = "USD",
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a user's product access record
    /// </summary>
    Task<UserProduct?> GetUserProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all products a user has access to
    /// </summary>
    Task<IEnumerable<UserProduct>> GetUserProductsAsync(
        Guid userId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has active access to a product
    /// </summary>
    Task<bool> HasProductAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a user's access to a product
    /// </summary>
    Task<bool> RevokeProductAccessAsync(
        Guid userId,
        Guid productId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extend a user's access to a product
    /// </summary>
    Task<bool> ExtendProductAccessAsync(
        Guid userId,
        Guid productId,
        DateTime newEndDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all product access records that are expiring before a threshold date
    /// </summary>
    Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(
        DateTime thresholdDate,
        CancellationToken cancellationToken = default);
}
