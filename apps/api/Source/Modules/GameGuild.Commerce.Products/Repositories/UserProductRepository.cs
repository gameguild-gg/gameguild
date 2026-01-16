using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for UserProduct entities
/// </summary>
public class UserProductRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<UserProduct>(context), IUserProductRepository
{
    /// <inheritdoc />
    public new async Task<UserProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Entities
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
        return await Entities
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
        var query = Entities
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
        var query = Entities.Where(up => up.ProductId == productId);

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
        return await Entities.AnyAsync(
            up => up.UserId == userId &&
                  up.ProductId == productId &&
                  up.AccessStatus == ProductAccessStatus.Active,
            cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(userProduct, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public new Task UpdateAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        Entities.Update(userProduct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserProduct userProduct, CancellationToken cancellationToken = default)
    {
        Entities.Remove(userProduct);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserProduct>> GetExpiringAccessAsync(
        DateTime thresholdDate,
        CancellationToken cancellationToken = default)
    {
        return await Entities
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
        return await Entities
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
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
