using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Products;

/// <summary>
/// Repository implementation for <see cref="PromoCampaign"/> entities (issue #418).
/// </summary>
public class PromoCampaignRepository(IApplicationDbContext context)
    : CommerceRepositoryBase<PromoCampaign>(context), IPromoCampaignRepository
{
    private DbSet<PromoCampaignAttribution> Attributions => Context.Set<PromoCampaignAttribution>();

    /// <inheritdoc />
    public new async Task<PromoCampaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Query
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PromoCampaign?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var normalized = key.Trim();
        return await Query
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> KeyExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var normalized = key.Trim();
        return await Query
            .AsNoTracking()
            .AnyAsync(c => c.Key == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<PromoCampaign> Items, int TotalCount)> GetPagedAsync(
        PromoCampaignStatus? status = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var query = Query.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(term)
                || c.Key.ToLower().Contains(term)
                || (c.Description != null && c.Description.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<int> CountCodesForKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        var normalized = key.Trim();
        return await Context.Set<PromoCode>()
            .AsNoTracking()
            .CountAsync(p => p.CampaignId == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> CountAttributionsForKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return 0;
        }

        var normalized = key.Trim();
        return await Attributions
            .AsNoTracking()
            .CountAsync(a => a.CampaignKey == normalized, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(PromoCampaign campaign, CancellationToken cancellationToken = default)
    {
        await Entities.AddAsync(campaign, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public new Task UpdateAsync(PromoCampaign campaign, CancellationToken cancellationToken = default)
    {
        Entities.Update(campaign);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DeleteAsync(PromoCampaign campaign, CancellationToken cancellationToken = default)
    {
        Entities.Remove(campaign);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
