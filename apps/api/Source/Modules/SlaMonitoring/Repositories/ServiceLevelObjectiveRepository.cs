using Microsoft.EntityFrameworkCore;
using GameGuild.Modules.SlaMonitoring.Entities;
using GameGuild.Infrastructure.Data;

namespace GameGuild.Modules.SlaMonitoring.Repositories;

/// <summary>
/// Repository implementation for Service Level Objectives using EF Core.
/// </summary>
public class ServiceLevelObjectiveRepository : IServiceLevelObjectiveRepository
{
    private readonly ApplicationDbContext _context;

    public ServiceLevelObjectiveRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceLevelObjective?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ServiceLevelObjectives
            .FirstOrDefaultAsync(slo => slo.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ServiceLevelObjective>> GetAllAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ServiceLevelObjectives.AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(slo => slo.TenantId == tenantId.Value);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        await _context.ServiceLevelObjectives.AddAsync(slo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ServiceLevelObjective slo, CancellationToken cancellationToken = default)
    {
        _context.ServiceLevelObjectives.Update(slo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var slo = await GetByIdAsync(id, cancellationToken);
        if (slo != null)
        {
            _context.ServiceLevelObjectives.Remove(slo);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
