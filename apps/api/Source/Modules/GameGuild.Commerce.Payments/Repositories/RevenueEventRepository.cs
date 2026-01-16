using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository for revenue events
/// </summary>
public class RevenueEventRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<RevenueEvent>(context), IRevenueEventRepository
{
    public new async Task<RevenueEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false); }

    public async Task<List<RevenueEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await Entities.Where(e => e.Timestamp >= startDate && e.Timestamp <= endDate).OrderByDescending(e => e.Timestamp).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<RevenueEvent>> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        return await Entities.Where(e => e.ReferenceId == referenceId).OrderByDescending(e => e.Timestamp).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default) { await Entities.AddAsync(revenueEvent, cancellationToken).ConfigureAwait(false); }

    public new async Task UpdateAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default)
    {
        Entities.Update(revenueEvent);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
