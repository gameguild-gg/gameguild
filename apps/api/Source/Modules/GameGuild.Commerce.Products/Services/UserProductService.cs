namespace GameGuild.Commerce.Products;

/// <summary>
/// Service for managing user product access
/// </summary>
public class UserProductService(IUserProductRepository userProductRepository) : IUserProductService
{
    /// <inheritdoc />
    public async Task<UserProduct> GrantProductAccessAsync(
        Guid userId,
        Guid productId,
        ProductAcquisitionType acquisitionType,
        decimal pricePaid = 0,
        string currency = "USD",
        DateTime? expiresAt = null,
        CancellationToken cancellationToken = default)
    {
        // Check if user already has access
        var existingAccess = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        if (existingAccess != null)
        {
            // Reactivate if expired or revoked
            if (existingAccess.AccessStatus != ProductAccessStatus.Active)
            {
                existingAccess.AccessStatus = ProductAccessStatus.Active;
                existingAccess.AccessEndDate = expiresAt;
                existingAccess.Touch();

                await userProductRepository.UpdateAsync(existingAccess, cancellationToken).ConfigureAwait(false);
                await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }

            return existingAccess;
        }

        var userProduct = new UserProduct
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProductId = productId,
            AcquisitionType = acquisitionType,
            AccessStatus = ProductAccessStatus.Active,
            PricePaid = pricePaid,
            Currency = currency,
            AccessStartDate = SystemClock.UtcNow,
            AccessEndDate = expiresAt
        };

        await userProductRepository.AddAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return userProduct;
    }

    /// <inheritdoc />
    public async Task<UserProduct?> GetUserProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetUserProductsAsync(
        Guid userId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        return await userProductRepository.GetByUserIdAsync(
            userId, status, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasProductAccessAsync(
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

        if (userProduct.AccessStatus != ProductAccessStatus.Active)
        {
            return false;
        }

        if (userProduct.AccessEndDate.HasValue && userProduct.AccessEndDate.Value < SystemClock.UtcNow)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RevokeProductAccessAsync(
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

        userProduct.AccessStatus = ProductAccessStatus.Revoked;
        userProduct.Touch();

        await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ExtendProductAccessAsync(
        Guid userId,
        Guid productId,
        DateTime newEndDate,
        CancellationToken cancellationToken = default)
    {
        var userProduct = await userProductRepository.GetByUserAndProductAsync(
            userId, productId, cancellationToken).ConfigureAwait(false);

        if (userProduct == null)
        {
            return false;
        }

        userProduct.AccessEndDate = newEndDate;
        userProduct.AccessStatus = ProductAccessStatus.Active;
        userProduct.Touch();

        await userProductRepository.UpdateAsync(userProduct, cancellationToken).ConfigureAwait(false);
        await userProductRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(
        DateTime thresholdDate,
        CancellationToken cancellationToken = default)
    {
        // Get all active access that expires before threshold
        var allActive = await userProductRepository.GetByUserIdAsync(
            Guid.Empty, // We need a different repository method for this
            ProductAccessStatus.Active,
            cancellationToken).ConfigureAwait(false);

        return allActive.Where(up =>
            up.AccessEndDate.HasValue &&
            up.AccessEndDate.Value <= thresholdDate);
    }
}
