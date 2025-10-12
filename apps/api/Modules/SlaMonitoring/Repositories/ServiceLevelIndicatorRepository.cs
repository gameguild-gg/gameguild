using GameGuild.Database;
using GameGuild.Modules.SlaMonitoring.Entities;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository implementation for Service Level Indicators using EF Core.
/// </summary>
public class ServiceLevelIndicatorRepository : IServiceLevelIndicatorRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceLevelIndicatorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(ServiceLevelIndicator sli, CancellationToken cancellationToken = default)
    {
        await _context.ServiceLevelIndicators.AddAsync(sli, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<ServiceLevelIndicator>> GetBySloIdAsync(Guid sloId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceLevelIndicators
            .Where(sli => sli.SloId == sloId && sli.Timestamp >= startDate && sli.Timestamp <= endDate)
            .OrderBy(sli => sli.Timestamp)
            .ToListAsync(cancellationToken);
    }
}
