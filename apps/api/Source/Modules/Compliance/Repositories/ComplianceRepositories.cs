using Microsoft.EntityFrameworkCore;
using GameGuild.Database;
using GameGuild.Core.Data;
using GameGuild.Modules.Compliance.Entities;

namespace GameGuild.Modules.Compliance.Repositories;

public class ConsentPolicyRepository : IConsentPolicyRepository
{
    private readonly ApplicationDbContext _context;

    public ConsentPolicyRepository(ApplicationDbContext context) => _context = context;

    public async Task<ConsentPolicy?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<ConsentPolicy>().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<List<ConsentPolicy>> GetAllAsync(Guid? tenantId, bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ConsentPolicy>().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(p => p.TenantId == tenantId.Value);

        if (!includeInactive)
            query = query.Where(p => p.IsActive);

        return await query.ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ConsentPolicy policy, CancellationToken cancellationToken = default)
    {
        await _context.Set<ConsentPolicy>().AddAsync(policy, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ConsentPolicy policy, CancellationToken cancellationToken = default)
    {
        _context.Set<ConsentPolicy>().Update(policy);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetByIdAsync(id, cancellationToken);
        if (policy != null)
        {
            _context.Set<ConsentPolicy>().Remove(policy);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}

public class PolicyVersionRepository : IPolicyVersionRepository
{
    private readonly ApplicationDbContext _context;

    public PolicyVersionRepository(ApplicationDbContext context) => _context = context;

    public async Task<PolicyVersion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<PolicyVersion>().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public async Task<List<PolicyVersion>> GetByPolicyIdAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        await _context.Set<PolicyVersion>().Where(v => v.PolicyId == policyId).OrderByDescending(v => v.CreatedAt).ToListAsync(cancellationToken);

    public async Task<PolicyVersion?> GetCurrentVersionAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        await _context.Set<PolicyVersion>().FirstOrDefaultAsync(v => v.PolicyId == policyId && v.IsCurrent, cancellationToken);

    public async Task CreateAsync(PolicyVersion version, CancellationToken cancellationToken = default)
    {
        await _context.Set<PolicyVersion>().AddAsync(version, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(PolicyVersion version, CancellationToken cancellationToken = default)
    {
        _context.Set<PolicyVersion>().Update(version);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class UserConsentRepository : IUserConsentRepository
{
    private readonly ApplicationDbContext _context;

    public UserConsentRepository(ApplicationDbContext context) => _context = context;

    public async Task<UserConsent?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<UserConsent>().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<List<UserConsent>> GetByUserIdAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<UserConsent>().Where(c => c.UserId == userId);

        if (tenantId.HasValue)
            query = query.Where(c => c.TenantId == tenantId.Value);

        return await query.OrderByDescending(c => c.ConsentedAt).ToListAsync(cancellationToken);
    }

    public async Task<List<UserConsent>> GetByPolicyIdAsync(Guid policyId, CancellationToken cancellationToken = default) =>
        await _context.Set<UserConsent>().Where(c => c.PolicyId == policyId).OrderByDescending(c => c.ConsentedAt).ToListAsync(cancellationToken);

    public async Task<UserConsent?> GetByUserAndPolicyAsync(Guid userId, Guid policyId, CancellationToken cancellationToken = default) =>
        await _context.Set<UserConsent>()
            .Where(c => c.UserId == userId && c.PolicyId == policyId && c.IsConsented)
            .OrderByDescending(c => c.ConsentedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task CreateAsync(UserConsent consent, CancellationToken cancellationToken = default)
    {
        await _context.Set<UserConsent>().AddAsync(consent, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserConsent consent, CancellationToken cancellationToken = default)
    {
        _context.Set<UserConsent>().Update(consent);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public class ComplianceAuditRepository : IComplianceAuditRepository
{
    private readonly ApplicationDbContext _context;

    public ComplianceAuditRepository(ApplicationDbContext context) => _context = context;

    public async Task<ComplianceAudit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Set<ComplianceAudit>().FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<List<ComplianceAudit>> GetAuditLogAsync(
        Guid? tenantId,
        Guid? userId,
        AuditEventType? eventType,
        DateTime? startDate,
        DateTime? endDate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<ComplianceAudit>().AsQueryable();

        if (tenantId.HasValue)
            query = query.Where(a => a.TenantId == tenantId.Value);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (eventType.HasValue)
            query = query.Where(a => a.EventType == eventType.Value);

        if (startDate.HasValue)
            query = query.Where(a => a.OccurredAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.OccurredAt <= endDate.Value);

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ComplianceAudit audit, CancellationToken cancellationToken = default)
    {
        await _context.Set<ComplianceAudit>().AddAsync(audit, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
