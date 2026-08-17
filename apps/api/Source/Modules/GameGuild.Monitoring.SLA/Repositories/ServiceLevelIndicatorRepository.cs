

using Microsoft.EntityFrameworkCore;

namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Repository implementation for Service Level Indicator (SLI metric) entities.
/// </summary>
public class ServiceLevelIndicatorRepository(DbContext context) : IServiceLevelIndicatorRepository
{
    private readonly DbSet<ServiceLevelIndicator> _metrics = context.Set<ServiceLevelIndicator>();

    public async Task<ServiceLevelIndicator?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _metrics.FirstOrDefaultAsync(m => m.Id == id, cancellationToken); }

    public async Task<List<ServiceLevelIndicator>> GetBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _metrics.Where(m => m.ServiceLevelObjectiveId == sloId).OrderByDescending(m => m.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelIndicator>> GetBySloIdAndTimeRangeAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        return await _metrics.Where(m => m.ServiceLevelObjectiveId == sloId && m.Timestamp >= startTime && m.Timestamp <= endTime).OrderByDescending(m => m.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<long> GetSuccessfulCountAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        return await _metrics.LongCountAsync(m => m.ServiceLevelObjectiveId == sloId && m.IsSuccessful && m.Timestamp >= startTime && m.Timestamp <= endTime, cancellationToken);
    }

    public async Task<long> GetTotalCountAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        return await _metrics.LongCountAsync(m => m.ServiceLevelObjectiveId == sloId && m.Timestamp >= startTime && m.Timestamp <= endTime, cancellationToken);
    }

    public async Task<List<ServiceLevelIndicator>> GetByEndpointAsync(Guid sloId, string endpoint, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default)
    {
        return await _metrics.Where(m => m.ServiceLevelObjectiveId == sloId && m.Endpoint == endpoint && m.Timestamp >= startTime && m.Timestamp <= endTime)
            .OrderByDescending(m => m.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceLevelIndicator> AddAsync(ServiceLevelIndicator metric, CancellationToken cancellationToken = default)
    {
        await _metrics.AddAsync(metric, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return metric;
    }

    public async Task AddRangeAsync(IEnumerable<ServiceLevelIndicator> metrics, CancellationToken cancellationToken = default)
    {
        await _metrics.AddRangeAsync(metrics, cancellationToken).ConfigureAwait(false);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldMetrics = await _metrics.Where(m => m.Timestamp < cutoffDate).ToListAsync(cancellationToken);

        _metrics.RemoveRange(oldMetrics);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<ServiceLevelIndicator>> GetRecentAsync(int count, CancellationToken cancellationToken = default)
    {
        return await _metrics.OrderByDescending(m => m.Timestamp).Take(count).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelIndicator>> GetSuccessfulAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _metrics.Where(m => m.ServiceLevelObjectiveId == sloId && m.IsSuccessful).OrderByDescending(m => m.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelIndicator>> GetFailedAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _metrics.Where(m => m.ServiceLevelObjectiveId == sloId && !m.IsSuccessful).OrderByDescending(m => m.Timestamp).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Guid sloId, CancellationToken cancellationToken = default) { return await _metrics.CountAsync(m => m.ServiceLevelObjectiveId == sloId, cancellationToken); }

    public async Task<int> CountSuccessfulAsync(Guid sloId, CancellationToken cancellationToken = default)
    {
        return await _metrics.CountAsync(m => m.ServiceLevelObjectiveId == sloId && m.IsSuccessful, cancellationToken);
    }

    public async Task<int> CountFailedAsync(Guid sloId, CancellationToken cancellationToken = default) { return await _metrics.CountAsync(m => m.ServiceLevelObjectiveId == sloId && !m.IsSuccessful, cancellationToken); }
}
