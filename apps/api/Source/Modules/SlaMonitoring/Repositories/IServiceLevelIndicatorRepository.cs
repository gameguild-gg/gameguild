using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository interface for Service Level Indicators.
/// </summary>
public interface IServiceLevelIndicatorRepository
{
    Task AddAsync(ServiceLevelIndicator sli, CancellationToken cancellationToken = default);
    Task<IEnumerable<ServiceLevelIndicator>> GetBySloIdAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}
