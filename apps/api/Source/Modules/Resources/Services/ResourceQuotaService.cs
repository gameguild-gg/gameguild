using System.Text.Json;
using GameGuild.Database;
using GameGuild.Modules.Resources.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Implementation of resource quota management service
/// </summary>
public class ResourceQuotaService : IResourceQuotaService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResourceQuotaService> _logger;

    public ResourceQuotaService(ApplicationDbContext context, ILogger<ResourceQuotaService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ResourceQuota> SetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        long? softLimit,
        long? hardLimit,
        ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly,
        CancellationToken cancellationToken = default)
    {
        var existingQuota = await _context.ResourceQuotas
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Type == type, cancellationToken);

        if (existingQuota != null)
        {
            existingQuota.SoftLimit = softLimit;
            existingQuota.HardLimit = hardLimit;
            existingQuota.Period = period;
            existingQuota.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            existingQuota = new ResourceQuota
            {
                TenantId = tenantId,
                Type = type,
                SoftLimit = softLimit,
                HardLimit = hardLimit,
                Period = period,
                LastReset = DateTime.UtcNow
            };
            _context.ResourceQuotas.Add(existingQuota);
        }

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Set quota for tenant {TenantId}, type {Type}: Soft={SoftLimit}, Hard={HardLimit}",
            tenantId, type, softLimit, hardLimit);

        return existingQuota;
    }

    public async Task<ResourceQuota?> GetQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResourceQuotas
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.Type == type, cancellationToken);
    }

    public async Task<IEnumerable<ResourceQuota>> GetTenantQuotasAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ResourceQuotas
            .Where(q => q.TenantId == tenantId)
            .OrderBy(q => q.Type)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteQuotaAsync(
        Guid tenantId,
        ResourceUsageType type,
        CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
        if (quota == null) return false;

        _context.ResourceQuotas.Remove(quota);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted quota for tenant {TenantId}, type {Type}", tenantId, type);
        return true;
    }

    public async Task<bool> RecordUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Update quota usage
            var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
            if (quota != null)
            {
                // Check if quota needs reset
                if (quota.ShouldReset())
                {
                    quota.Reset();
                }

                quota.CurrentUsage += amount;
                quota.UpdatedAt = DateTime.UtcNow;
            }

            // Record usage history
            var today = DateTime.UtcNow.Date;
            var usageRecord = await _context.ResourceUsageRecords
                .FirstOrDefaultAsync(r => r.TenantId == tenantId &&
                                         r.Type == type &&
                                         r.PeriodStart == today, cancellationToken);

            if (usageRecord != null)
            {
                usageRecord.Count += amount;
                usageRecord.UpdatedAt = DateTime.UtcNow;

                // Update peak usage if this is higher
                if (usageRecord.PeakUsage == null || usageRecord.Count > usageRecord.PeakUsage)
                {
                    usageRecord.PeakUsage = usageRecord.Count;
                    usageRecord.PeakUsageDate = DateTime.UtcNow;
                }
            }
            else
            {
                usageRecord = ResourceUsageRecord.CreateDaily(type, tenantId, amount, today, userId, source);
                usageRecord.PeakUsage = amount;
                usageRecord.PeakUsageDate = DateTime.UtcNow;

                if (metadata != null)
                {
                    usageRecord.Metadata = JsonSerializer.Serialize(metadata);
                }

                _context.ResourceUsageRecords.Add(usageRecord);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Recorded usage for tenant {TenantId}, type {Type}: amount={Amount}",
                tenantId, type, amount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording usage for tenant {TenantId}, type {Type}", tenantId, type);
            return false;
        }
    }

    public async Task<long> GetCurrentUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota?.ShouldReset() == true)
        {
            quota.Reset();
            await _context.SaveChangesAsync(cancellationToken);
            return 0;
        }

        return quota?.CurrentUsage ?? 0;
    }

    public async Task<IEnumerable<ResourceUsageRecord>> GetUsageHistoryAsync(
        Guid tenantId,
        ResourceUsageType type,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceUsageRecords
            .Where(r => r.TenantId == tenantId && r.Type == type);

        if (fromDate.HasValue)
            query = query.Where(r => r.PeriodStart >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(r => r.PeriodEnd <= toDate.Value);

        return await query
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<ResourceLimitCheckResponse> CheckLimitsAsync(
        Guid tenantId,
        ResourceUsageType type,
        long requestedAmount = 1,
        CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);

        if (quota == null)
        {
            // No quota means unlimited
            return ResourceLimitCheckResponse.Success(type, requestedAmount, null, null, "No quota set - unlimited");
        }

        // Check if quota needs reset
        if (quota.ShouldReset())
        {
            quota.Reset();
            await _context.SaveChangesAsync(cancellationToken);
        }

        var currentUsage = quota.CurrentUsage;
        var projectedUsage = currentUsage + requestedAmount;

        // Check hard limit
        if (quota.HardLimit.HasValue && projectedUsage > quota.HardLimit.Value)
        {
            return ResourceLimitCheckResponse.LimitExceeded(
                type, currentUsage, quota.SoftLimit, quota.HardLimit,
                $"Hard limit exceeded. Requested: {requestedAmount}, Current: {currentUsage}, Limit: {quota.HardLimit}");
        }

        // Check soft limit
        var softLimitExceeded = quota.SoftLimit.HasValue && projectedUsage > quota.SoftLimit.Value;
        var message = softLimitExceeded
            ? $"Soft limit exceeded but within hard limit. Requested: {requestedAmount}, Current: {currentUsage}, Soft Limit: {quota.SoftLimit}"
            : "Within limits";

        return ResourceLimitCheckResponse.Success(type, currentUsage, quota.SoftLimit, quota.HardLimit, message);
    }

    public async Task<Dictionary<ResourceUsageType, ResourceLimitCheckResponse>> CheckMultipleLimitsAsync(
        Guid tenantId,
        Dictionary<ResourceUsageType, long> requestedAmounts,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<ResourceUsageType, ResourceLimitCheckResponse>();

        foreach (var kvp in requestedAmounts)
        {
            results[kvp.Key] = await CheckLimitsAsync(tenantId, kvp.Key, kvp.Value, cancellationToken);
        }

        return results;
    }

    public async Task<ResourceLimitCheckResponse> TryConsumeResourceAsync(
        Guid tenantId,
        ResourceUsageType type,
        long amount = 1,
        Guid? userId = null,
        string? source = null,
        CancellationToken cancellationToken = default)
    {
        var limitCheck = await CheckLimitsAsync(tenantId, type, amount, cancellationToken);

        if (limitCheck.CanProceed)
        {
            await RecordUsageAsync(tenantId, type, amount, userId, source, cancellationToken: cancellationToken);
            _logger.LogDebug("Successfully consumed {Amount} units of {Type} for tenant {TenantId}",
                amount, type, tenantId);
        }
        else
        {
            _logger.LogWarning("Failed to consume {Amount} units of {Type} for tenant {TenantId}: {Reason}",
                amount, type, tenantId, limitCheck.Message);
        }

        return limitCheck;
    }

    public async Task<MultiResourceUsageResponse> GetTenantUsageOverviewAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var quotas = await GetTenantQuotasAsync(tenantId, cancellationToken);
        var response = new MultiResourceUsageResponse { TenantId = tenantId };

        foreach (var quota in quotas)
        {
            var usage = await GetResourceUsageDetailsAsync(tenantId, quota.Type, 30, cancellationToken);
            response.Usage[quota.Type] = usage;

            if (usage.HardLimit.HasValue && usage.CurrentUsage >= usage.HardLimit.Value)
            {
                response.HardLimitExceeded.Add(quota.Type);
                response.HasExceededLimits = true;
            }
            else if (usage.SoftLimit.HasValue && usage.CurrentUsage >= usage.SoftLimit.Value)
            {
                response.SoftLimitExceeded.Add(quota.Type);
            }
        }

        return response;
    }

    public async Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(
        Guid tenantId,
        ResourceUsageType type,
        int historyDays = 30,
        CancellationToken cancellationToken = default)
    {
        var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
        var fromDate = DateTime.UtcNow.AddDays(-historyDays);
        var history = await GetUsageHistoryAsync(tenantId, type, fromDate, null, cancellationToken);

        var response = new ResourceUsageResponse
        {
            Type = type,
            CurrentUsage = quota?.CurrentUsage ?? 0,
            SoftLimit = quota?.SoftLimit,
            HardLimit = quota?.HardLimit,
            Period = quota?.Period ?? ResourceQuotaPeriod.Monthly,
            LastReset = quota?.LastReset,
            IsActive = quota?.IsActive ?? false,
            History = history.Select(h => new ResourceUsageHistoryItem
            {
                Date = h.PeriodStart,
                Count = h.Count,
                PeakUsage = h.PeakUsage
            }).ToList()
        };

        if (quota?.HardLimit.HasValue == true && quota.HardLimit.Value > 0)
        {
            response.UsagePercentage = (double)response.CurrentUsage / quota.HardLimit.Value * 100;
            response.RemainingQuota = Math.Max(0, quota.HardLimit.Value - response.CurrentUsage);
        }

        return response;
    }

    public async Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(
        ResourceUsageType? type = null,
        bool hardLimitOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ResourceQuotas.AsQueryable();

        if (type.HasValue)
            query = query.Where(q => q.Type == type.Value);

        if (hardLimitOnly)
        {
            query = query.Where(q => q.HardLimit.HasValue && q.CurrentUsage >= q.HardLimit.Value);
        }
        else
        {
            query = query.Where(q =>
                (q.HardLimit.HasValue && q.CurrentUsage >= q.HardLimit.Value) ||
                (q.SoftLimit.HasValue && q.CurrentUsage >= q.SoftLimit.Value));
        }

        return await query.Select(q => q.TenantId).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<int> ResetExpiredQuotasAsync(CancellationToken cancellationToken = default)
    {
        var expiredQuotas = await _context.ResourceQuotas
            .Where(q => q.IsActive && q.LastReset.HasValue)
            .ToListAsync(cancellationToken);

        var resetCount = 0;
        foreach (var quota in expiredQuotas.Where(q => q.ShouldReset()))
        {
            quota.Reset();
            resetCount++;
        }

        if (resetCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Reset {Count} expired quotas", resetCount);
        }

        return resetCount;
    }

    public async Task<int> CleanupOldUsageRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var oldRecords = await _context.ResourceUsageRecords
            .Where(r => r.PeriodEnd < olderThan)
            .ToListAsync(cancellationToken);

        if (oldRecords.Count > 0)
        {
            _context.ResourceUsageRecords.RemoveRange(oldRecords);
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Cleaned up {Count} old usage records older than {Date}", oldRecords.Count, olderThan);
        }

        return oldRecords.Count;
    }

    public async Task<bool> RecalculateUsageAsync(
        Guid tenantId,
        ResourceUsageType type,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var quota = await GetQuotaAsync(tenantId, type, cancellationToken);
            if (quota == null) return false;

            // Get current period start based on quota period and last reset
            var periodStart = quota.LastReset ?? DateTime.UtcNow.Date;

            var totalUsage = await _context.ResourceUsageRecords
                .Where(r => r.TenantId == tenantId && r.Type == type && r.PeriodStart >= periodStart)
                .SumAsync(r => r.Count, cancellationToken);

            quota.CurrentUsage = totalUsage;
            quota.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Recalculated usage for tenant {TenantId}, type {Type}: {Usage}",
                tenantId, type, totalUsage);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating usage for tenant {TenantId}, type {Type}", tenantId, type);
            return false;
        }
    }
}
