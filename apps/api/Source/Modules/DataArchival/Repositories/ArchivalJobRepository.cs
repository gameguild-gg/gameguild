using GameGuild.Modules.DataArchival.Entities;


namespace GameGuild.Modules.DataArchival.Repositories;

/// <summary>
/// Repository interface for ArchivalJob.
/// </summary>
public interface IArchivalJobRepository
{
    Task<ArchivalJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ArchivalJob>> GetAllAsync(Guid? tenantId = null, Guid? policyId = null, CancellationToken cancellationToken = default);
    Task AddAsync(ArchivalJob job, CancellationToken cancellationToken = default);
    Task UpdateAsync(ArchivalJob job, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository implementation for ArchivalJob.
/// </summary>
public class ArchivalJobRepository : IArchivalJobRepository
{
    private readonly DbContext _context;

    public ArchivalJobRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<ArchivalJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ArchivalJob>()
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }

    public async Task<List<ArchivalJob>> GetAllAsync(Guid? tenantId = null, Guid? policyId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ArchivalJob>().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(j => j.TenantId == tenantId.Value);

        if (policyId.HasValue)
            query = query.Where(j => j.PolicyId == policyId.Value);

        return await query.OrderByDescending(j => j.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ArchivalJob job, CancellationToken cancellationToken = default)
    {
        await _context.Set<ArchivalJob>().AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ArchivalJob job, CancellationToken cancellationToken = default)
    {
        _context.Set<ArchivalJob>().Update(job);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
