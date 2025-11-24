using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Enums;

namespace GameGuild.Monitoring.SLA.Abstractions;

/// <summary>
///     Repository interface for Service Level Objectives
/// </summary>
public interface IServiceLevelObjectiveRepository
{
    /// <summary>
    ///     Gets an SLO by ID
    /// </summary>
    Task<ServiceLevelObjective?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an SLO by ID with related indicators
    /// </summary>
    Task<ServiceLevelObjective?> GetByIdWithIndicatorsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets an SLO by ID with violations
    /// </summary>
    Task<ServiceLevelObjective?> GetByIdWithViolationsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all SLOs for a tenant
    /// </summary>
    Task<List<ServiceLevelObjective>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets SLOs by service name
    /// </summary>
    Task<List<ServiceLevelObjective>> GetByServiceNameAsync(string serviceName, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets enabled SLOs for evaluation
    /// </summary>
    Task<List<ServiceLevelObjective>> GetEnabledSlosAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets SLOs by status
    /// </summary>
    Task<List<ServiceLevelObjective>> GetByStatusAsync(SloStatus status, Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Adds a new SLO
    /// </summary>
    Task<ServiceLevelObjective> AddAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Updates an existing SLO
    /// </summary>
    Task UpdateAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Deletes an SLO
    /// </summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an SLO exists by name
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default);
}
