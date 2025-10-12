using GameGuild.Database;


namespace GameGuild.Modules.Tenants.Repositories;

public class TenantArchivalRepository : ITenantArchivalRepository
{
    private readonly ApplicationDbContext _context;

    public TenantArchivalRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    // Policies
    public async Task<TenantArchivalPolicy> CreatePolicyAsync(TenantArchivalPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.Set<TenantArchivalPolicy>().AddAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<TenantArchivalPolicy> UpdatePolicyAsync(TenantArchivalPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantArchivalPolicy>().Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
        return policy;
    }

    public async Task<TenantArchivalPolicy?> GetPolicyByIdAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchivalPolicy>()
            .FirstOrDefaultAsync(p => p.Id == policyId, cancellationToken);
    }

    public async Task<TenantArchivalPolicy?> GetPolicyByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchivalPolicy>()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId, cancellationToken);
    }

    public async Task<List<TenantArchivalPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchivalPolicy>()
            .Where(p => p.IsEnabled)
            .ToListAsync(cancellationToken);
    }

    public async Task DeletePolicyAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await GetPolicyByIdAsync(policyId, cancellationToken);
        if (policy != null)
        {
            _context.Set<TenantArchivalPolicy>().Remove(policy);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Archive Records
    public async Task<TenantArchiveRecord> CreateArchiveRecordAsync(TenantArchiveRecord record, CancellationToken cancellationToken = default)
    {
        await _context.Set<TenantArchiveRecord>().AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<TenantArchiveRecord> UpdateArchiveRecordAsync(TenantArchiveRecord record, CancellationToken cancellationToken = default)
    {
        _context.Set<TenantArchiveRecord>().Update(record);
        await _context.SaveChangesAsync(cancellationToken);
        return record;
    }

    public async Task<TenantArchiveRecord?> GetArchiveRecordByIdAsync(Guid recordId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchiveRecord>()
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
    }

    public async Task<TenantArchiveRecord?> GetArchiveRecordByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchiveRecord>()
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.ArchivedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<TenantArchiveRecord>> GetArchiveRecordsByStatusAsync(TenantArchivalStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Set<TenantArchiveRecord>()
            .Where(r => r.Status == status)
            .ToListAsync(cancellationToken);
    }
}
