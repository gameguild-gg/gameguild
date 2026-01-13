using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for UserProduct entities
/// </summary>
public class UserProductRepository(IApplicationDbContext context) : IUserProductRepository
{
    private DbSet<UserProduct> UserProducts => context.Set<UserProduct>();

    /// <inheritdoc />
    public async Task<UserProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await UserProducts
            .Include(up => up.Product)
            .FirstOrDefaultAsync(up => up.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<UserProduct?> GetByUserAndProductAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await UserProducts
            .Include(up => up.Product)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.ProductId == productId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetByUserIdAsync(
        Guid userId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = UserProducts
            .Include(up => up.Product)
            .Where(up => up.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(up => up.AccessStatus == status.Value);
        }

        return await query
            .OrderByDescending(up => up.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetByProductIdAsync(
        Guid productId,
        ProductAccessStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = UserProducts.Where(up => up.ProductId == productId);

        if (status.HasValue)
        {
            query = query.Where(up => up.AccessStatus == status.Value);
        }

        return await query
            .OrderByDescending(up => up.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> HasAccessAsync(
        Guid userId,
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        return await UserProducts.AnyAsync(
            up => up.UserId == userId &&
                  up.ProductId == productId &&
                  up.AccessStatus == ProductAccessStatus.Active,
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        await UserProducts.AddAsync(userProduct, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        UserProducts.Update(userProduct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        UserProducts.Remove(userProduct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(
        DateTime thresholdDate,
        CancellationToken cancellationToken = default)
    {
        return await UserProducts
            .Include(up => up.Product)
            .Where(up => up.AccessStatus == ProductAccessStatus.Active &&
                         up.AccessEndDate != null &&
                         up.AccessEndDate <= thresholdDate &&
                         up.AccessEndDate > DateTime.UtcNow)
            .OrderBy(up => up.AccessEndDate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetExpiredSubscriptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return await UserProducts
            .Include(up => up.Product)
            .Where(up => up.AccessStatus == ProductAccessStatus.Active &&
                         up.AcquisitionType == ProductAcquisitionType.Subscription &&
                         up.AccessEndDate != null &&
                         up.AccessEndDate <= DateTime.UtcNow)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
