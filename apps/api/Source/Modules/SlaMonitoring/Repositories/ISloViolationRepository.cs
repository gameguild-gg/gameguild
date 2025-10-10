using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository interface for SLO Violations.
/// </summary>
public interface ISloViolationRepository
{
    Task<SloViolation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(SloViolation violation, CancellationToken cancellationToken = default);
    Task UpdateAsync(SloViolation violation, CancellationToken cancellationToken = default);
    Task<IEnumerable<SloViolation>> GetBySloIdAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<SloViolation>> GetActiveBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SloViolation>> GetActiveViolationsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
}
