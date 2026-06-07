using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.FERPA;

public interface IFerpaEducationRecordRepository
{
    Task<FerpaEducationRecord> AddAsync(FerpaEducationRecord record, CancellationToken ct = default);
    Task<List<FerpaEducationRecord>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default);
    Task<List<FerpaEducationRecord>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default);
}

public interface IFerpaDirectoryInformationPolicyRepository
{
    Task<FerpaDirectoryInformationPolicy?> GetByTenantAsync(Guid? tenantId, CancellationToken ct = default);
    Task<FerpaDirectoryInformationPolicy> AddAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default);
    Task UpdateAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default);
}

public interface IFerpaDisclosureConsentRepository
{
    Task<FerpaDisclosureConsent> AddAsync(FerpaDisclosureConsent consent, CancellationToken ct = default);
    Task<FerpaDisclosureConsent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FerpaDisclosureConsent?> GetActiveAsync(Guid studentUserId, string recipient, string scope, DateTime instant, CancellationToken ct = default);
    Task<List<FerpaDisclosureConsent>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default);
    Task UpdateAsync(FerpaDisclosureConsent consent, CancellationToken ct = default);
}

public interface IFerpaDisclosureLogRepository
{
    Task<FerpaDisclosureLog> AddAsync(FerpaDisclosureLog log, CancellationToken ct = default);
    Task<List<FerpaDisclosureLog>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default);
}

public interface IFerpaInspectionRequestRepository
{
    Task<FerpaInspectionRequest> AddAsync(FerpaInspectionRequest request, CancellationToken ct = default);
    Task<FerpaInspectionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<FerpaInspectionRequest>> GetPendingAsync(CancellationToken ct = default);
    Task UpdateAsync(FerpaInspectionRequest request, CancellationToken ct = default);
}

public sealed class FerpaEducationRecordRepository(IApplicationDbContext context) : IFerpaEducationRecordRepository
{
    public async Task<FerpaEducationRecord> AddAsync(FerpaEducationRecord record, CancellationToken ct = default)
    {
        var entry = await context.Set<FerpaEducationRecord>().AddAsync(record, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<List<FerpaEducationRecord>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
        => await context.Set<FerpaEducationRecord>()
            .Where(record => record.StudentUserId == studentUserId && record.DeletedAt == null)
            .OrderByDescending(record => record.CreatedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task<List<FerpaEducationRecord>> GetDirectoryInformationAsync(Guid studentUserId, CancellationToken ct = default)
        => await context.Set<FerpaEducationRecord>()
            .Where(record => record.StudentUserId == studentUserId && record.IsDirectoryInformation && record.DeletedAt == null)
            .OrderBy(record => record.Title)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class FerpaDirectoryInformationPolicyRepository(IApplicationDbContext context) : IFerpaDirectoryInformationPolicyRepository
{
    public async Task<FerpaDirectoryInformationPolicy?> GetByTenantAsync(Guid? tenantId, CancellationToken ct = default)
        => await context.Set<FerpaDirectoryInformationPolicy>()
            .FirstOrDefaultAsync(policy => policy.TenantId == tenantId && policy.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<FerpaDirectoryInformationPolicy> AddAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default)
    {
        var entry = await context.Set<FerpaDirectoryInformationPolicy>().AddAsync(policy, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task UpdateAsync(FerpaDirectoryInformationPolicy policy, CancellationToken ct = default)
    {
        policy.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class FerpaDisclosureConsentRepository(IApplicationDbContext context) : IFerpaDisclosureConsentRepository
{
    public async Task<FerpaDisclosureConsent> AddAsync(FerpaDisclosureConsent consent, CancellationToken ct = default)
    {
        var entry = await context.Set<FerpaDisclosureConsent>().AddAsync(consent, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<FerpaDisclosureConsent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<FerpaDisclosureConsent>()
            .FirstOrDefaultAsync(consent => consent.Id == id && consent.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<FerpaDisclosureConsent?> GetActiveAsync(Guid studentUserId, string recipient, string scope, DateTime instant, CancellationToken ct = default)
        => await context.Set<FerpaDisclosureConsent>()
            .FirstOrDefaultAsync(consent =>
                consent.StudentUserId == studentUserId &&
                consent.Recipient == recipient &&
                consent.Scope == scope &&
                consent.RevokedAt == null &&
                consent.EffectiveFrom <= instant &&
                (!consent.ExpiresAt.HasValue || consent.ExpiresAt.Value >= instant) &&
                consent.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<List<FerpaDisclosureConsent>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
        => await context.Set<FerpaDisclosureConsent>()
            .Where(consent => consent.StudentUserId == studentUserId && consent.DeletedAt == null)
            .OrderByDescending(consent => consent.EffectiveFrom)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task UpdateAsync(FerpaDisclosureConsent consent, CancellationToken ct = default)
    {
        consent.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class FerpaDisclosureLogRepository(IApplicationDbContext context) : IFerpaDisclosureLogRepository
{
    public async Task<FerpaDisclosureLog> AddAsync(FerpaDisclosureLog log, CancellationToken ct = default)
    {
        var entry = await context.Set<FerpaDisclosureLog>().AddAsync(log, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<List<FerpaDisclosureLog>> GetByStudentAsync(Guid studentUserId, CancellationToken ct = default)
        => await context.Set<FerpaDisclosureLog>()
            .Where(log => log.StudentUserId == studentUserId && log.DeletedAt == null)
            .OrderByDescending(log => log.DisclosedAt)
            .ToListAsync(ct)
            .ConfigureAwait(false);
}

public sealed class FerpaInspectionRequestRepository(IApplicationDbContext context) : IFerpaInspectionRequestRepository
{
    public async Task<FerpaInspectionRequest> AddAsync(FerpaInspectionRequest request, CancellationToken ct = default)
    {
        var entry = await context.Set<FerpaInspectionRequest>().AddAsync(request, ct).ConfigureAwait(false);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
        return entry.Entity;
    }

    public async Task<FerpaInspectionRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await context.Set<FerpaInspectionRequest>()
            .FirstOrDefaultAsync(request => request.Id == id && request.DeletedAt == null, ct)
            .ConfigureAwait(false);

    public async Task<List<FerpaInspectionRequest>> GetPendingAsync(CancellationToken ct = default)
        => await context.Set<FerpaInspectionRequest>()
            .Where(request => request.Status == FerpaRequestStatus.Pending && request.DeletedAt == null)
            .OrderBy(request => request.Deadline)
            .ToListAsync(ct)
            .ConfigureAwait(false);

    public async Task UpdateAsync(FerpaInspectionRequest request, CancellationToken ct = default)
    {
        request.Touch();
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
