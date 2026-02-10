using Microsoft.Extensions.Logging;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Service for managing product entitlements
/// </summary>
public class EntitlementService(
    IUserProductRepository userProductRepository,
    IProductRepository productRepository,
    ILogger<EntitlementService> logger) : IEntitlementService
{
    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        return userProduct?.HasActiveAccess() ?? false;
    }

    /// <inheritdoc />
    public async Task<IDictionary<Guid, bool>> HasAccessAsync(
        Guid userId,
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<Guid, bool>();
        var userProducts = await userProductRepository.GetByUserIdAsync(
            userId, ProductAccessStatus.Active, cancellationToken).ConfigureAwait(false);

        var activeProductIds = userProducts
            .Where(up => up.HasActiveAccess())
            .Select(up => up.ProductId)
            .ToHashSet();

        foreach (var productId in productIds)
        {
            result[productId] = activeProductIds.Contains(productId);
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EntitlementInfo>> GetUserEntitlementsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var userProducts = await userProductRepository.GetByUserIdAsync(
            userId, cancellationToken: cancellationToken).ConfigureAwait(false);

        return userProducts.Select(up => new EntitlementInfo(
            up.ProductId,
            up.Product?.Name ?? "Unknown",
            up.AccessStatus,
            up.AcquisitionType,
            up.AccessStartDate,
            up.AccessEndDate,
            up.AcquisitionType == ProductAcquisitionType.Subscription,
            up.SubscriptionStatus,
            up.PricePaid,
            up.Currency));
    }

    /// <inheritdoc />
    public async Task<EntitlementResult> GrantEntitlementAsync(
        Guid userId,
        Guid productId,
        ProductAcquisitionType acquisitionType,
        decimal pricePaid = 0,
        string currency = "USD",
        DateTime? expiresAt = null,
        Guid? orderId = null,
        CancellationToken cancellationToken = default)
    {
        // Economic Model: Warn when entitlements are granted without Order reference
        // This creates unauditable entitlements and should only occur for admin corrections or migrations
        if (!orderId.HasValue)
        {
            logger.LogWarning(
                "Entitlement granted without OrderId for User {UserId}, Product {ProductId}. " +
                "This bypasses audit trail and should only occur for admin corrections or legacy migrations.",
                userId, productId);
        }

        // Check if already has access
        var existingAccess = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        if (existingAccess != null && existingAccess.HasActiveAccess())
        {
            return EntitlementResult.Succeeded(existingAccess, alreadyHadAccess: true);
        }

        // Get product to verify it exists
        var product = await productRepository.GetByIdAsync(productId, cancellationToken).ConfigureAwait(false);
        if (product == null)
        {
            return EntitlementResult.Failed($"Product {productId} not found");
        }

        if (existingAccess != null)
        {
            // Reactivate existing access
            existingAccess.GrantAccess(expiresAt, pricePaid, currency, acquisitionType);
            existingAccess.OrderId = orderId;
            existingAccess.Touch();

            await userProductRepository.UpdateAsync(existingAccess, cancellationToken).ConfigureAwait(false);
            await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return EntitlementResult.Succeeded(existingAccess, alreadyHadAccess: false);
        }

        // Create new entitlement
        var userProduct = UserProduct.Create(
            userId,
            productId,
            acquisitionType,
            pricePaid,
            currency,
            expiresAt,
            product.TenantId);

        userProduct.OrderId = orderId;

        if (acquisitionType == ProductAcquisitionType.Subscription)
        {
            userProduct.SubscriptionStatus = EntitlementSubscriptionStatus.Active;
        }

        await userProductRepository.AddAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return EntitlementResult.Succeeded(userProduct);
    }

    /// <inheritdoc />
    public async Task<bool> RevokeEntitlementAsync(
        Guid userId,
        Guid productId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        if (userProduct == null)
        {
            return false;
        }

        userProduct.RevokeAccess(reason);
        userProduct.Touch();

        await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateSubscriptionAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        if (userProduct == null)
        {
            return false;
        }

        if (userProduct.AcquisitionType != ProductAcquisitionType.Subscription)
        {
            return userProduct.HasActiveAccess();
        }

        // Check if subscription has expired
        if (userProduct.AccessEndDate.HasValue && userProduct.AccessEndDate.Value < SystemClock.UtcNow)
        {
            userProduct.AccessStatus = ProductAccessStatus.Expired;
            userProduct.SubscriptionStatus = EntitlementSubscriptionStatus.Expired;
            userProduct.Touch();

            await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
            await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return false;
        }

        return userProduct.HasActiveAccess();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EntitlementInfo>> GetExpiringEntitlementsAsync(
        int daysUntilExpiration = 7,
        CancellationToken cancellationToken = default)
    {
        var threshold = SystemClock.UtcNow.AddDays(daysUntilExpiration);
        var expiringProducts = await userProductRepository.GetExpiringAccessAsync(threshold, cancellationToken).ConfigureAwait(false);

        return expiringProducts.Select(up => new EntitlementInfo(
            up.ProductId,
            up.Product?.Name ?? "Unknown",
            up.AccessStatus,
            up.AcquisitionType,
            up.AccessStartDate,
            up.AccessEndDate,
            up.AcquisitionType == ProductAcquisitionType.Subscription,
            up.SubscriptionStatus,
            up.PricePaid,
            up.Currency));
    }

    /// <inheritdoc />
    public async Task<int> ProcessExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var expiredProducts = await userProductRepository.GetExpiredSubscriptionsAsync(cancellationToken).ConfigureAwait(false);
        var count = 0;

        foreach (var userProduct in expiredProducts)
        {
            userProduct.AccessStatus = ProductAccessStatus.Expired;
            userProduct.SubscriptionStatus = EntitlementSubscriptionStatus.Expired;
            userProduct.Touch();

            await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
            count++;
        }

        if (count > 0)
        {
            await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return count;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EntitlementInfo>> GetAllActiveEntitlementsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeProducts = await userProductRepository.GetAllActiveAsync(cancellationToken).ConfigureAwait(false);

        return activeProducts.Select(up => new EntitlementInfo(
            up.ProductId,
            up.Product?.Name ?? "Unknown",
            up.AccessStatus,
            up.AcquisitionType,
            up.AccessStartDate,
            up.AccessEndDate,
            up.AcquisitionType == ProductAcquisitionType.Subscription,
            up.SubscriptionStatus,
            up.PricePaid,
            up.Currency));
    }
}
