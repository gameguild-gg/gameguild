using GameGuild.Modules.Payments.Payments.Domain.Entities;

namespace GameGuild.Modules.Payments.Payments.Application.Repositories;

public interface IRevenueEventRepository
{
    Task<RevenueEvent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<RevenueEvent>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, int skip, int take, CancellationToken cancellationToken = default);
    Task<List<RevenueEvent>> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task AddAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default);
    Task UpdateAsync(RevenueEvent revenueEvent, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
