using GameGuild.Modules.DataArchival.Entities;


namespace GameGuild.Modules.DataArchival.Repositories;

/// <summary>
/// Repository interface for ArchivalPolicy.
/// </summary>
public interface IArchivalPolicyRepository
{
    Task<ArchivalPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<List<ArchivalPolicy>> GetAllAsync(Guid? tenantId = null, string? entityType = null, CancellationToken cancellationToken = default);
    Task AddAsync(ArchivalPolicy policy, CancellationToken cancellationToken = default);
    Task UpdateAsync(ArchivalPolicy policy, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository implementation for ArchivalPolicy.
/// </summary>
public class ArchivalPolicyRepository : IArchivalPolicyRepository
{
    private readonly DbContext _context;

    public ArchivalPolicyRepository(DbContext context)
    {
        _context = context;
    }

    public async Task<ArchivalPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Set<ArchivalPolicy>()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<List<ArchivalPolicy>> GetAllAsync(Guid? tenantId = null, string? entityType = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ArchivalPolicy>().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(p => p.TenantId == tenantId.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(p => p.EntityType == entityType);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ArchivalPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.Set<ArchivalPolicy>().AddAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ArchivalPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.Set<ArchivalPolicy>().Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetByIdAsync(id, cancellationToken);
        if (policy == null)
            return false;

        _context.Set<ArchivalPolicy>().Remove(policy);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
