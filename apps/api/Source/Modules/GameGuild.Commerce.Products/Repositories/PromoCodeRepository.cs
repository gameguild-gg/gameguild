using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for PromoCode entities
/// </summary>
public class PromoCodeRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<PromoCode>(context), IPromoCodeRepository
{
    private DbSet<PromoCodeUse> PromoCodeUses => Context.Set<PromoCodeUse>();

    /// <inheritdoc />
    public async Task<PromoCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PromoCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Include(p => p.Product)
            .FirstOrDefaultAsync(p => p.Code == code, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PromoCode>> GetActiveCodesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await Entities
            .Where(p => p.IsActive)
            .Where(p => p.ValidFrom == null || p.ValidFrom <= now)
            .Where(p => p.ValidUntil == null || p.ValidUntil > now)
            .OrderByDescending(p => p.StackingPriority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PromoCode>> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await Entities
            .Where(p => p.ProductId == productId || p.ProductId == null)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.StackingPriority)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<PromoCode> Items, int TotalCount)> GetPagedAsync(
        bool? isActive = null,
        PromoCodeType? type = null,
        Guid? productId = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = Entities.AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (productId.HasValue)
        {
            query = query.Where(p => p.ProductId == productId.Value || p.ProductId == null);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(p =>
                p.Code.ToLower().Contains(term) ||
                p.Name.ToLower().Contains(term) ||
                (p.Description != null && p.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> GetUsageCountAsync(Guid promoCodeId, CancellationToken cancellationToken = default)
    {
        return await PromoCodeUses
            .CountAsync(u => u.PromoCodeId == promoCodeId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> GetUserUsageCountAsync(Guid promoCodeId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await PromoCodeUses
            .CountAsync(u => u.PromoCodeId == promoCodeId && u.UserId == userId, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(promoCode, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        Entities.Update(promoCode);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(PromoCode promoCode, CancellationToken cancellationToken = default)
    {
        Entities.Remove(promoCode);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task RecordUsageAsync(PromoCodeUse usage, CancellationToken cancellationToken = default)
    {
        await PromoCodeUses.AddAsync(usage, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Entities.AnyAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        return await Entities.AnyAsync(p => p.Code == code, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
