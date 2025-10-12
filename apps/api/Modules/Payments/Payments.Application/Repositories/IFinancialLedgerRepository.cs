using GameGuild.Modules.Payments.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Payments.Application.Repositories;

public interface IFinancialLedgerRepository
{
    Task<FinancialLedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<FinancialLedgerEntry>> GetByAccountAsync(string account, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<FinancialLedgerEntry>> GetUnreconciledAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task AddAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(FinancialLedgerEntry entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
