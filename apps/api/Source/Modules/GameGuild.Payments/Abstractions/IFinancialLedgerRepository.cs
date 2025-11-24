using GameGuild.Payments.Entities;

namespace GameGuild.Payments.Abstractions;

/// <summary>
///     Repository for financial ledger entries
/// </summary>
public interface IFinancialLedgerRepository
{
    /// <summary>Get ledger entry by ID</summary>
    Task<FinancialLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Get ledger entries by account</summary>
    Task<List<FinancialLedgerEntry>> GetByAccountAsync(string account, int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Get unreconciled ledger entries</summary>
    Task<List<FinancialLedgerEntry>> GetUnreconciledAsync(int skip, int take, CancellationToken cancellationToken = default);

    /// <summary>Add new ledger entry</summary>
    Task AddAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Update ledger entry</summary>
    Task UpdateAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Save changes to database</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
