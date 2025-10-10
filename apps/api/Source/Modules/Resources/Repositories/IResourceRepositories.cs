using GameGuild.Modules.Resources.Entities;

namespace GameGuild.Modules.Resources.Repositories;

/// <summary>
/// Repository interface for ResourceUsageRecord operations
/// </summary>
public interface IResourceUsageRepository
{
    /// <summary>
    /// Get usage records for a tenant with optional filtering
    /// </summary>
    Task<IEnumerable<ResourceUsageRecord>> GetUsageRecordsAsync(
        Guid tenantId,
        ResourceUsageType? usageType = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Get current usage summary for a tenant
    /// </summary>
    Task<Dictionary<ResourceUsageType, long>> GetCurrentUsageSummaryAsync(Guid tenantId);

    /// <summary>
    /// Add a usage record
    /// </summary>
    Task<ResourceUsageRecord> AddAsync(ResourceUsageRecord usageRecord);

    /// <summary>
    /// Update a usage record
    /// </summary>
    Task<ResourceUsageRecord> UpdateAsync(ResourceUsageRecord usageRecord);

    /// <summary>
    /// Delete a usage record
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Get usage record by ID
    /// </summary>
    Task<ResourceUsageRecord?> GetByIdAsync(Guid id);
}

/// <summary>
/// Repository interface for ResourceQuota operations
/// </summary>
public interface IResourceQuotaRepository
{
    /// <summary>
    /// Get quota for a specific tenant and usage type
    /// </summary>
    Task<ResourceQuota?> GetQuotaAsync(Guid tenantId, ResourceUsageType usageType);

    /// <summary>
    /// Get all quotas for a tenant with optional filtering
    /// </summary>
    Task<IEnumerable<ResourceQuota>> GetQuotasByTenantIdAsync(Guid tenantId, ResourceUsageType? usageType = null);

    /// <summary>
    /// Add a quota
    /// </summary>
    Task<ResourceQuota> AddAsync(ResourceQuota quota);

    /// <summary>
    /// Update a quota
    /// </summary>
    Task<ResourceQuota> UpdateAsync(ResourceQuota quota);

    /// <summary>
    /// Delete a quota
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Get quota by ID
    /// </summary>
    Task<ResourceQuota?> GetByIdAsync(Guid id);

    /// <summary>
    /// Check if tenant has exceeded any quotas
    /// </summary>
    Task<bool> HasExceededQuotaAsync(Guid tenantId, ResourceUsageType usageType);
}