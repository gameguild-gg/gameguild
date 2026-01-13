using GameGuild.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Resources;

/// <summary>
///     Repository implementation for UsageRecord entity
/// </summary>
public class UsageRecordRepository(IApplicationDbContext context) : IUsageRecordRepository
{
    private DbSet<UsageRecord> UsageRecords { get => context.Set<UsageRecord>(); }

    public async Task<UsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) { return await UsageRecords.FirstOrDefaultAsync(r => r.Id == id, cancellationToken); }

    public async Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.TenantId!.Value == tenantId).OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageRecord>> GetByTenantAsync(Guid tenantId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = UsageRecords.Where(r => r.TenantId!.Value == tenantId);

        if (type.HasValue) query = query.Where(r => r.Type == type.Value);

        if (fromDate.HasValue) query = query.Where(r => r.PeriodStart >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(r => r.PeriodStart <= toDate.Value);

        return await query.OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageRecord>> GetByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.TenantId!.Value == tenantId && r.Type == type).OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageRecord>> GetByDateRangeAsync(Guid tenantId, ResourceUsageType type, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.TenantId!.Value == tenantId && r.Type == type && r.PeriodStart >= fromDate && r.PeriodStart <= toDate)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsageRecord> CreateAsync(UsageRecord record, CancellationToken cancellationToken = default)
    {
        UsageRecords.Add(record);
        await context.SaveChangesAsync(cancellationToken);

        return record;
    }

    public async Task<UsageRecord> AddAsync(UsageRecord record, CancellationToken cancellationToken = default) { return await CreateAsync(record, cancellationToken); }

    public async Task<UsageRecord> UpdateAsync(UsageRecord record, CancellationToken cancellationToken = default)
    {
        UsageRecords.Update(record);
        await context.SaveChangesAsync(cancellationToken);

        return record;
    }

    public async Task<long> GetCurrentUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.TenantId!.Value == tenantId && r.Type == type).SumAsync(r => r.UsageAmount, cancellationToken);
    }

    public async Task<bool> DeleteOldRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var records = await UsageRecords.Where(r => r.PeriodStart < olderThan).ToListAsync(cancellationToken);

        if (records.Count == 0) return false;

        UsageRecords.RemoveRange(records);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default) { return await DeleteOldRecordsAsync(cutoffDate, cancellationToken); }

    public async Task<IEnumerable<UsageRecord>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.UserId == userId).OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<long> CalculateTotalUsageAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.TenantId!.Value == tenantId && r.Type == type).SumAsync(r => r.UsageAmount, cancellationToken);
    }

    public async Task<int> ArchiveOlderThanAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var records = await UsageRecords.Where(r => r.PeriodStart < cutoffDate).ToListAsync(cancellationToken);

        // Mark records for archiving (no IsArchived property, so we'll track metadata)
        var archived = 0;

        foreach (var record in records)
        {
            // Update metadata to indicate archived status
            var metadata = string.IsNullOrEmpty(record.Metadata) ? "{\"archived\":true}" : record.Metadata.TrimEnd('}') + ",\"archived\":true}";

            record.Metadata = metadata;
            archived++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return archived;
    }

    public async Task<bool> DeleteByTenantAndTypeAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var records = await UsageRecords.Where(r => r.TenantId!.Value == tenantId && r.Type == type).ToListAsync(cancellationToken);

        if (records.Count == 0) return false;

        UsageRecords.RemoveRange(records);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    public async Task<bool> DeleteByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var records = await UsageRecords.Where(r => r.TenantId!.Value == tenantId).ToListAsync(cancellationToken);

        if (records.Count == 0) return false;

        UsageRecords.RemoveRange(records);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }

    // User-level methods
    public async Task<IEnumerable<UsageRecord>> GetByUserAsync(Guid userId, ResourceUsageType? type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = UsageRecords.Where(r => r.UserId == userId);

        if (type.HasValue) query = query.Where(r => r.Type == type.Value);

        if (fromDate.HasValue) query = query.Where(r => r.PeriodStart >= fromDate.Value);

        if (toDate.HasValue) query = query.Where(r => r.PeriodStart <= toDate.Value);

        return await query.OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageRecord>> GetByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.UserId == userId && r.Type == type).OrderByDescending(r => r.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UsageRecord>> GetByUserDateRangeAsync(Guid userId, ResourceUsageType type, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.UserId == userId && r.Type == type && r.PeriodStart >= fromDate && r.PeriodStart <= toDate)
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<long> GetCurrentUserUsageAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        return await UsageRecords.Where(r => r.UserId == userId && r.Type == type).SumAsync(r => r.UsageAmount, cancellationToken);
    }

    public async Task<bool> DeleteByUserAndTypeAsync(Guid userId, ResourceUsageType type, CancellationToken cancellationToken = default)
    {
        var records = await UsageRecords.Where(r => r.UserId == userId && r.Type == type).ToListAsync(cancellationToken);

        if (records.Count == 0) return false;

        UsageRecords.RemoveRange(records);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
