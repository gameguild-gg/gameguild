using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Consent;

public interface IConsentPolicyRepository
{
    Task<ConsentPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<ConsentPolicy>> GetAllActiveAsync(Guid? tenantId, CancellationToken ct = default);
    Task<ConsentPolicy> AddAsync(ConsentPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(ConsentPolicy policy, CancellationToken ct = default);
}

public interface IPolicyVersionRepository
{
    Task<PolicyVersion?> GetCurrentVersionAsync(Guid policyId, CancellationToken ct = default);
    Task<PolicyVersion> AddAsync(PolicyVersion version, CancellationToken ct = default);
}

public interface IUserConsentRepository
{
    Task<UserConsent?> GetAsync(Guid userId, Guid policyVersionId, CancellationToken ct = default);
    Task<List<UserConsent>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserConsent> AddAsync(UserConsent consent, CancellationToken ct = default);
    Task UpdateAsync(UserConsent consent, CancellationToken ct = default);
}

public interface IDataSubjectRequestRepository
{
    Task<DataSubjectRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<DataSubjectRequest>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<DataSubjectRequest>> GetPendingAsync(CancellationToken ct = default);
    Task<DataSubjectRequest> AddAsync(DataSubjectRequest request, CancellationToken ct = default);
    Task UpdateAsync(DataSubjectRequest request, CancellationToken ct = default);
}

public class ConsentPolicyRepository(IApplicationDbContext context) : IConsentPolicyRepository
{
    public async Task<ConsentPolicy?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<ConsentPolicy>().Include(p => p.Versions)
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null, ct).ConfigureAwait(false);

    public async Task<List<ConsentPolicy>> GetAllActiveAsync(Guid? tenantId, CancellationToken ct = default)
    {
        var query = context.Set<ConsentPolicy>()
            .Include(p => p.Versions.Where(v => v.IsCurrent))
            .Where(p => p.IsActive && p.DeletedAt == null);
        if (tenantId.HasValue) query = query.Where(p => p.TenantId == tenantId.Value);
        return await query.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<ConsentPolicy> AddAsync(ConsentPolicy policy, CancellationToken ct = default)
    {
        var entry = await context.Set<ConsentPolicy>().AddAsync(policy, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(ConsentPolicy policy, CancellationToken ct = default)
    {
        policy.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public class PolicyVersionRepository(IApplicationDbContext context) : IPolicyVersionRepository
{
    public async Task<PolicyVersion?> GetCurrentVersionAsync(Guid policyId, CancellationToken ct = default)
        => await context.Set<PolicyVersion>()
            .FirstOrDefaultAsync(v => v.ConsentPolicyId == policyId && v.IsCurrent && v.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<PolicyVersion> AddAsync(PolicyVersion version, CancellationToken ct = default)
    {
        var entry = await context.Set<PolicyVersion>().AddAsync(version, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }
}

public class UserConsentRepository(IApplicationDbContext context) : IUserConsentRepository
{
    public async Task<UserConsent?> GetAsync(Guid userId, Guid policyVersionId, CancellationToken ct = default)
        => await context.Set<UserConsent>()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.PolicyVersionId == policyVersionId && c.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<List<UserConsent>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Set<UserConsent>().Include(c => c.PolicyVersion)
            .Where(c => c.UserId == userId && c.DeletedAt == null).ToListAsync(ct).ConfigureAwait(false);

    public async Task<UserConsent> AddAsync(UserConsent consent, CancellationToken ct = default)
    {
        var entry = await context.Set<UserConsent>().AddAsync(consent, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(UserConsent consent, CancellationToken ct = default)
    {
        consent.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public class DataSubjectRequestRepository(IApplicationDbContext context) : IDataSubjectRequestRepository
{
    public async Task<DataSubjectRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<DataSubjectRequest>()
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null, ct).ConfigureAwait(false);

    public async Task<List<DataSubjectRequest>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => await context.Set<DataSubjectRequest>()
            .Where(r => r.UserId == userId && r.DeletedAt == null)
            .OrderByDescending(r => r.CreatedAt).ToListAsync(ct).ConfigureAwait(false);

    public async Task<List<DataSubjectRequest>> GetPendingAsync(CancellationToken ct = default)
        => await context.Set<DataSubjectRequest>()
            .Where(r => r.Status == DataSubjectRequestStatus.Pending && r.DeletedAt == null)
            .OrderBy(r => r.Deadline).ToListAsync(ct).ConfigureAwait(false);

    public async Task<DataSubjectRequest> AddAsync(DataSubjectRequest request, CancellationToken ct = default)
    {
        var entry = await context.Set<DataSubjectRequest>().AddAsync(request, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(DataSubjectRequest request, CancellationToken ct = default)
    {
        request.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
