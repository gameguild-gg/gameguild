using GameGuild.Monitoring.SLA.Abstractions;
using GameGuild.Monitoring.SLA.Entities;
using GameGuild.Monitoring.SLA.Enums;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Monitoring.SLA.Repositories;

/// <summary>
///     Repository implementation for Service Level Objective entities.
/// </summary>
public abstract class ServiceLevelObjectiveRepository(DbContext context) : IServiceLevelObjectiveRepository
{
    private readonly DbSet<ServiceLevelObjective> _slos = context.Set<ServiceLevelObjective>();

    public async Task<ServiceLevelObjective?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await _slos.FirstOrDefaultAsync(slo => slo.Id == id, cancellationToken); }

    public async Task<ServiceLevelObjective?> GetByIdWithIndicatorsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _slos.Include("Indicators").FirstOrDefaultAsync(slo => slo.Id == id, cancellationToken);
    }

    public async Task<ServiceLevelObjective?> GetByIdWithViolationsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _slos.Include("Violations").FirstOrDefaultAsync(slo => slo.Id == id, cancellationToken);
    }

    public async Task<List<ServiceLevelObjective>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _slos.Where(slo => slo.TenantId != null && slo.TenantId.Value == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelObjective>> GetByServiceNameAsync(string serviceName, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _slos.Where(slo => slo.ServiceName == serviceName && slo.TenantId != null && slo.TenantId.Value == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelObjective>> GetEnabledSlosAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _slos.Where(slo => slo.IsEnabled && slo.TenantId != null && slo.TenantId.Value == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<List<ServiceLevelObjective>> GetByStatusAsync(SloStatus status, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _slos.Where(slo => slo.Status == status && slo.TenantId != null && slo.TenantId.Value == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<ServiceLevelObjective> AddAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        await _slos.AddAsync(slo, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        return slo;
    }

    public async Task UpdateAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        _slos.Update(slo);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slo = await GetByIdAsync(id, cancellationToken);

        if (slo != null)
        {
            _slos.Remove(slo);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _slos.AnyAsync(slo => slo.Name == name && slo.TenantId != null && slo.TenantId.Value == tenantId, cancellationToken);
    }
}
