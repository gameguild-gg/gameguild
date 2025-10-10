using GameGuild.Database;
using GameGuild.Modules.Tenants.Abstractions;
using GameGuild.Modules.Tenants.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Tenants.Services;

/// <summary>
///     Service for tracking and managing tenant resource usage
/// </summary>
public class UsageTrackingService : IUsageTrackingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UsageTrackingService> _logger;

    public UsageTrackingService(ApplicationDbContext dbContext, ILogger<UsageTrackingService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<UsageTracking> TrackUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        long amount,
        string? customResourceName = null,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            // Create new usage tracking entry
            usage = new UsageTracking
            {
                TenantId = tenantId,
                ResourceType = resourceType,
                CustomResourceName = customResourceName,
                CurrentUsage = amount,
                LastUpdatedAt = DateTime.UtcNow,
                PeriodStartedAt = DateTime.UtcNow
            };

            _dbContext.Set<UsageTracking>().Add(usage);
            _logger.LogInformation("Created new usage tracking for tenant {TenantId}, resource {ResourceType}",
                tenantId, resourceType);
        }
        else
        {
            // Update existing usage
            usage.IncrementUsage(amount);
            _dbContext.Set<UsageTracking>().Update(usage);
            _logger.LogDebug("Updated usage tracking for tenant {TenantId}, resource {ResourceType}",
                tenantId, resourceType);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return usage;
    }

    public async Task<UsageTracking> IncrementUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        long amount,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            throw new InvalidOperationException(
                $"Usage tracking not found for tenant {tenantId} and resource {resourceType}");
        }

        usage.IncrementUsage(amount);
        _dbContext.Set<UsageTracking>().Update(usage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Incremented usage by {Amount} for tenant {TenantId}, resource {ResourceType}",
            amount, tenantId, resourceType);

        return usage;
    }

    public async Task<bool> CheckLimitExceededAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            return false; // No usage tracked = no limit exceeded
        }

        var isExceeded = usage.IsLimitExceeded;

        if (isExceeded)
        {
            _logger.LogWarning("Usage limit exceeded for tenant {TenantId}, resource {ResourceType}: {CurrentUsage}/{UsageLimit}",
                tenantId, resourceType, usage.CurrentUsage, usage.UsageLimit);
        }

        return isExceeded;
    }

    public async Task<UsageTracking?> GetUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UsageTracking>()
            .FirstOrDefaultAsync(u => u.TenantId == tenantId && u.ResourceType == resourceType, cancellationToken);
    }

    public async Task<IReadOnlyList<UsageTracking>> GetAllUsageAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<UsageTracking>()
            .Where(u => u.TenantId == tenantId)
            .OrderBy(u => u.ResourceType)
            .ToListAsync(cancellationToken);
    }

    public async Task ResetUsageAsync(
        Guid tenantId,
        ResourceType resourceType,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            _logger.LogWarning("Attempted to reset non-existent usage tracking for tenant {TenantId}, resource {ResourceType}",
                tenantId, resourceType);
            return;
        }

        usage.ResetUsage();
        _dbContext.Set<UsageTracking>().Update(usage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Reset usage for tenant {TenantId}, resource {ResourceType}",
            tenantId, resourceType);
    }

    public async Task UpdateLimitAsync(
        Guid tenantId,
        ResourceType resourceType,
        long newLimit,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            throw new InvalidOperationException(
                $"Usage tracking not found for tenant {tenantId} and resource {resourceType}");
        }

        usage.UpdateLimit(newLimit);
        _dbContext.Set<UsageTracking>().Update(usage);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated usage limit for tenant {TenantId}, resource {ResourceType}: {NewLimit}",
            tenantId, resourceType, newLimit);
    }

    public async Task<bool> IsWithinLimitAsync(
        Guid tenantId,
        ResourceType resourceType,
        decimal bufferPercentage = 0,
        CancellationToken cancellationToken = default)
    {
        var usage = await GetUsageAsync(tenantId, resourceType, cancellationToken);

        if (usage == null)
        {
            return true; // No usage tracked = within limit
        }

        return usage.IsWithinLimit(bufferPercentage);
    }
}
