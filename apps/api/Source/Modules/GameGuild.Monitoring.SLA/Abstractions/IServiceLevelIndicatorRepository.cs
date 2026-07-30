
namespace GameGuild.Monitoring.SLA;

/// <summary>
///     Repository interface for Service Level Indicators
/// </summary>
public interface IServiceLevelIndicatorRepository
{
    /// <summary>
    ///     Gets an SLI by ID
    /// </summary>
    Task<ServiceLevelIndicator?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all SLIs for an SLO
    /// </summary>
    Task<List<ServiceLevelIndicator>> GetBySloIdAsync(Guid sloId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets SLIs for an SLO within a time range
    /// </summary>
    Task<List<ServiceLevelIndicator>> GetBySloIdAndTimeRangeAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets successful SLIs count for an SLO in a time range
    /// </summary>
    Task<long> GetSuccessfulCountAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets total SLIs count for an SLO in a time range
    /// </summary>
    Task<long> GetTotalCountAsync(Guid sloId, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets SLIs by endpoint
    /// </summary>
    Task<List<ServiceLevelIndicator>> GetByEndpointAsync(Guid sloId, string endpoint, DateTimeOffset startTime, DateTimeOffset endTime, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new SLI
    /// </summary>
    Task<ServiceLevelIndicator> AddAsync(ServiceLevelIndicator indicator, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds multiple SLIs
    /// </summary>
    Task AddRangeAsync(IEnumerable<ServiceLevelIndicator> indicators, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes old SLIs beyond retention period
    /// </summary>
    Task DeleteOlderThanAsync(DateTimeOffset cutoffDate, CancellationToken cancellationToken = default);
}
