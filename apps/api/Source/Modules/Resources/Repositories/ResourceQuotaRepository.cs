using GameGuild.Database;

namespace GameGuild.Modules.Resources;

/// <summary>
///     Entity Framework implementation for resource quota data access operations
/// </summary>
public class ResourceQuotaRepository(ApplicationDbContext context) : IResourceQuotaRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceQuotas.FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Type == type, cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceQuota>> GetTenantQuotasAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var quotas = await _context.ResourceQuotas.Where(q => q.TenantId == tenantId).OrderBy(q => q.Type).ToListAsync(cancellationToken);

        return quotas.AsReadOnly();
    }

    public async Task<ResourceQuota> CreateQuotaAsync(ResourceQuota quota, CancellationToken cancellationToken = default)
    {
        _ = _context.ResourceQuotas.Add(quota);
        await _context.SaveChangesAsync(cancellationToken);

        return quota;
    }

    public async Task<ResourceQuota> UpdateQuotaAsync(ResourceQuota quota, CancellationToken cancellationToken = default)
    {
        _ = _context.ResourceQuotas.Update(quota);
        await _context.SaveChangesAsync(cancellationToken);

        return quota;
    }

    public async Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        ResourceQuota? quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null) { return false; }

        _ = _context.ResourceQuotas.Remove(quota);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<ResourceUsageRecord?> GetUsageRecordAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceUsageRecords.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Type == type && r.PeriodStart == periodStart, cancellationToken);
    }

    public async Task<ResourceUsageRecord> CreateUsageRecordAsync(ResourceUsageRecord usageRecord, CancellationToken cancellationToken = default)
    {
        _ = _context.ResourceUsageRecords.Add(usageRecord);
        await _context.SaveChangesAsync(cancellationToken);

        return usageRecord;
    }

    public async Task<ResourceUsageRecord> UpdateUsageRecordAsync(ResourceUsageRecord usageRecord, CancellationToken cancellationToken = default)
    {
        _ = _context.ResourceUsageRecords.Update(usageRecord);
        await _context.SaveChangesAsync(cancellationToken);

        return usageRecord;
    }

    public async Task<IReadOnlyList<ResourceUsageRecord>> GetUsageHistoryAsync(Guid tenantId, ResourceUsageType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceUsageRecords.Where(r => r.TenantId == tenantId && r.Type == type);

        if (fromDate.HasValue) { query = query.Where(r => r.PeriodStart >= fromDate.Value); }

        if (toDate.HasValue) { query = query.Where(r => r.PeriodEnd <= toDate.Value); }

        var records = await query.OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);

        return records.AsReadOnly();
    }

    public async Task<IReadOnlyList<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceQuotas.AsQueryable();

        if (type.HasValue) { query = query.Where(q => q.Type == type.Value); }

        if (hardLimitOnly) { query = query.Where(q => q.HardLimit.HasValue && q.CurrentUsage >= q.HardLimit.Value); }
        else { query = query.Where(q => q.HardLimit.HasValue && q.CurrentUsage >= q.HardLimit.Value || q.SoftLimit.HasValue && q.CurrentUsage >= q.SoftLimit.Value); }

        var tenantIds = await query.Select(q => q.TenantId).Distinct().ToListAsync(cancellationToken);

        return tenantIds.AsReadOnly();
    }

    public async Task<IReadOnlyList<ResourceQuota>> GetActiveQuotasWithLastResetAsync(CancellationToken cancellationToken = default)
    {
        var quotas = await _context.ResourceQuotas.Where(q => q.IsActive && q.LastReset.HasValue).ToListAsync(cancellationToken);

        return quotas.AsReadOnly();
    }

    public async Task<IReadOnlyList<ResourceUsageRecord>> GetOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var records = await _context.ResourceUsageRecords.Where(r => r.PeriodEnd < olderThan).ToListAsync(cancellationToken);

        return records.AsReadOnly();
    }

    public async Task RemoveUsageRecordsAsync(IEnumerable<ResourceUsageRecord> records, CancellationToken cancellationToken = default)
    {
        _context.ResourceUsageRecords.RemoveRange(records);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<long> GetTotalUsageAsync(Guid tenantId, ResourceUsageType type, DateTime periodStart, CancellationToken cancellationToken = default)
    {
        return await _context.ResourceUsageRecords.Where(r => r.TenantId == tenantId && r.Type == type && r.PeriodStart >= periodStart).SumAsync(r => r.Count, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) { await _context.SaveChangesAsync(cancellationToken); }
}
