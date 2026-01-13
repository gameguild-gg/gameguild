using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Repository for financial ledger entries
/// </summary>
public class FinancialLedgerRepository(IApplicationDbContext context) : IFinancialLedgerRepository
{
    private DbSet<FinancialLedgerEntry> FinancialLedgerEntries { get => context.Set<FinancialLedgerEntry>(); }

    public async Task<FinancialLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await FinancialLedgerEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<FinancialLedgerEntry>> GetByAccountAsync(string account, int skip, int take, CancellationToken cancellationToken = default)
    {
        return await FinancialLedgerEntries.Where(e => e.DebitAccount == account || e.CreditAccount == account)
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<List<FinancialLedgerEntry>> GetUnreconciledAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await FinancialLedgerEntries.Where(e => !e.IsReconciled).OrderByDescending(e => e.CreatedAt).Skip(skip).Take(take).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default) { await FinancialLedgerEntries.AddAsync(entry, cancellationToken).ConfigureAwait(false); }

    public async Task UpdateAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default)
    {
        FinancialLedgerEntries.Update(entry);
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false); }
}
