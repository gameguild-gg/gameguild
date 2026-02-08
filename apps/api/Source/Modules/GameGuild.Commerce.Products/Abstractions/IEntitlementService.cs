namespace GameGuild.Commerce.Products;

/// <summary>
/// Service interface for managing product entitlements
/// </summary>
public interface IEntitlementService
{
    /// <summary>
    /// Check if a user has active access to a product
    /// </summary>
    Task<bool> HasAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a user has access to multiple products
    /// </summary>
    Task<IDictionary<Guid, bool>> HasAccessAsync(
        Guid userId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active entitlements for a user
    /// </summary>
    Task<IEnumerable<EntitlementInfo>> GetUserEntitlementsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Grant entitlement (typically called after order completion)
    /// </summary>
    Task<EntitlementResult> GrantEntitlementAsync(
        Guid userId,
        Guid productId,
        ProductAcquisitionType acquisitionType,
        decimal pricePaid = 0,
        string currency = "USD",
        DateTime? expiresAt = null,
        Guid? orderId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke entitlement
    /// </summary>
    Task<bool> RevokeEntitlementAsync(
        Guid userId,
        Guid productId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate subscription status and update if expired
    /// </summary>
    Task<bool> ValidateSubscriptionAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get entitlements expiring soon (for notifications)
    /// </summary>
    Task<IEnumerable<EntitlementInfo>> GetExpiringEntitlementsAsync(
        int daysUntilExpiration = 7,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all currently active entitlements across all users (admin use case)
    /// </summary>
    Task<IEnumerable<EntitlementInfo>> GetAllActiveEntitlementsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Process expired subscriptions (batch job)
    /// </summary>
    Task<int> ProcessExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a user's entitlement to a product
/// </summary>
public record EntitlementInfo(
    Guid ProductId,
    string ProductName,
    ProductAccessStatus Status,
    ProductAcquisitionType AcquisitionType,
    DateTime? AccessStartDate,
    DateTime? AccessEndDate,
    bool IsSubscription,
    EntitlementSubscriptionStatus? SubscriptionStatus,
    decimal PricePaid,
    string Currency);

/// <summary>
/// Result of an entitlement operation
/// </summary>
public class EntitlementResult
{
    public bool Success { get; init; }
    public UserProduct? UserProduct { get; init; }
    public string? ErrorMessage { get; init; }
    public bool AlreadyHadAccess { get; init; }

    public static EntitlementResult Succeeded(UserProduct userProduct, bool alreadyHadAccess = false)
        => new() { Success = true, UserProduct = userProduct, AlreadyHadAccess = alreadyHadAccess };

    public static EntitlementResult Failed(string message)
        => new() { Success = false, ErrorMessage = message };
}
