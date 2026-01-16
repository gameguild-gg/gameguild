using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository for financial ledger entries
/// </summary>
public class FinancialLedgerRepository(IApplicationDbContext context) 
    : CommerceRepositoryBase<FinancialLedgerEntry>(context), IFinancialLedgerRepository
{
    public new async Task<FinancialLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await Entities.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<FinancialLedgerEntry>> GetByAccountAsync(string account, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await Entities.Where(e => e.DebitAccount == account || e.CreditAccount == account)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<FinancialLedgerEntry>> GetUnreconciledAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await Entities.Where(e => !e.IsReconciled).OrderByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default) { await Entities.AddAsync(entry, cancellationToken).ConfigureAwait(false); }

    public new async Task UpdateAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        Entities.Update(entry);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await Context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
