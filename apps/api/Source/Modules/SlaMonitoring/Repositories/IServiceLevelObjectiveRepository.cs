using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository interface for Service Level Objectives.
/// </summary>
public interface IServiceLevelObjectiveRepository
{
    Task<ServiceLevelObjective?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceLevelObjective>> GetAllAsync(Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default);
    Task UpdateAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
