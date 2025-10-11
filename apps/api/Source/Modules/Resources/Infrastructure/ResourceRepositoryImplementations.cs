using GameGuild.Database;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Resources.Infrastructure;

/// <summary>
/// Repository implementation for ResourceUsageRecord operations
/// </summary>
public class ResourceUsageRepository : IResourceUsageRepository
{
    private readonly IApplicationDbContext _context;

    public ResourceUsageRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get usage records for a tenant with optional filtering
    /// </summary>
    public async Task<IEnumerable<ResourceUsageRecord>> GetUsageRecordsAsync(
        Guid tenantId,
        ResourceUsageType? usageType = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId);

        if (usageType.HasValue)
            query = query.Where(r => r.Type == usageType.Value);

        if (startDate.HasValue)
            query = query.Where(r => r.PeriodStart >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.PeriodEnd <= endDate.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get current usage summary for a tenant
    /// </summary>
    public async Task<Dictionary<ResourceUsageType, long>> GetCurrentUsageSummaryAsync(Guid tenantId)
    {
        var currentPeriodStart = DateTime.UtcNow.Date;
        var currentPeriodEnd = currentPeriodStart.AddDays(1).AddTicks(-1);

        var usageRecords = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId)
            .Where(r => r.PeriodStart >= currentPeriodStart && r.PeriodEnd <= currentPeriodEnd)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, TotalCount = g.Sum(r => r.Count) })
            .ToListAsync();

        return usageRecords.ToDictionary(x => x.Type, x => x.TotalCount);
    }

    /// <summary>
    /// Add a usage record
    /// </summary>
    public async Task<ResourceUsageRecord> AddAsync(ResourceUsageRecord usageRecord)
    {
        _context.Set<ResourceUsageRecord>().Add(usageRecord);
        await _context.SaveChangesAsync();
        return usageRecord;
    }

    /// <summary>
    /// Update a usage record
    /// </summary>
    public async Task<ResourceUsageRecord> UpdateAsync(ResourceUsageRecord usageRecord)
    {
        _context.Set<ResourceUsageRecord>().Update(usageRecord);
        await _context.SaveChangesAsync();
        return usageRecord;
    }

    /// <summary>
    /// Delete a usage record
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var record = await _context.Set<ResourceUsageRecord>()
            .FirstOrDefaultAsync(r => r.Id == id);

        if (record != null)
        {
            _context.Set<ResourceUsageRecord>().Remove(record);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get usage record by ID
    /// </summary>
    public async Task<ResourceUsageRecord?> GetByIdAsync(Guid id)
    {
        return await _context.Set<ResourceUsageRecord>()
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}

/// <summary>
/// Repository implementation for ResourceQuota operations
/// </summary>
public class ResourceQuotaRepository : IResourceQuotaRepository
{
    private readonly ApplicationDbContext _context;

    public ResourceQuotaRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get quota for a specific tenant and usage type
    /// </summary>
    public async Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType usageType)
    {
        return await _context.Set<ResourceQuota>()
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Type == usageType);
    }

    /// <summary>
    /// Get all quotas for a tenant with optional filtering
    /// </summary>
    public async Task<IEnumerable<ResourceQuota>> GetQuotasByTenantIdAsync(Guid tenantId, ResourceUsageType? usageType = null)
    {
        var query = _context.Set<ResourceQuota>()
            .Where(q => q.TenantId == tenantId);

        if (usageType.HasValue)
            query = query.Where(q => q.Type == usageType.Value);

        return await query.ToListAsync();
    }

    /// <summary>
    /// Add a quota
    /// </summary>
    public async Task<ResourceQuota> AddAsync(ResourceQuota quota)
    {
        _context.Set<ResourceQuota>().Add(quota);
        await _context.SaveChangesAsync();
        return quota;
    }

    /// <summary>
    /// Update a quota
    /// </summary>
    public async Task<ResourceQuota> UpdateAsync(ResourceQuota quota)
    {
        _context.Set<ResourceQuota>().Update(quota);
        await _context.SaveChangesAsync();
        return quota;
    }

    /// <summary>
    /// Delete a quota
    /// </summary>
    public async Task DeleteAsync(Guid id)
    {
        var quota = await _context.Set<ResourceQuota>()
            .FirstOrDefaultAsync(q => q.Id == id);

        if (quota != null)
        {
            _context.Set<ResourceQuota>().Remove(quota);
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get quota by ID
    /// </summary>
    public async Task<ResourceQuota?> GetByIdAsync(Guid id)
    {
        return await _context.Set<ResourceQuota>()
            .FirstOrDefaultAsync(q => q.Id == id);
    }

    /// <summary>
    /// Check if tenant has exceeded any quotas
    /// </summary>
    public async Task<bool> HasExceededQuotaAsync(Guid tenantId, ResourceUsageType usageType)
    {
        var quota = await GetQuotaAsync(tenantId, usageType);
        return quota != null && quota.HardLimit.HasValue && quota.CurrentUsage > quota.HardLimit.Value;
    }
}