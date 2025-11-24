using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Entities;
using GameGuild.Resources.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Services;

/// <summary>
///     Implementation of usage data retention and archival
/// </summary>
public class UsageRetentionService(IUsageRetentionPolicyRepository policyRepository, IUsageRecordRepository usageRepository, ILogger<UsageRetentionService> logger) : IUsageRetentionService
{
    public async Task<UsageRetentionPolicy> SetPolicyAsync(
        Guid? tenantId,
        ResourceUsageType? resourceType,
        int retentionDays,
        int archiveAfterDays,
        bool enableCompaction = true,
        CancellationToken cancellationToken = default
    )
    {
        var existingPolicy = await policyRepository.GetByTenantAndTypeAsync(tenantId, resourceType, cancellationToken);

        if (existingPolicy != null)
        {
            existingPolicy.RetentionDays = retentionDays;
            existingPolicy.ArchiveAfterDays = archiveAfterDays;
            existingPolicy.EnableCompaction = enableCompaction;
            existingPolicy.UpdatedAt = DateTime.UtcNow;
            existingPolicy = await policyRepository.UpdateAsync(existingPolicy, cancellationToken);
        }
        else
        {
            existingPolicy = new UsageRetentionPolicy
            {
                Name = $"Retention Policy - {(tenantId.HasValue ? $"Tenant {tenantId}" : "Global")} - {(resourceType.HasValue ? resourceType.ToString() : "All Types")}",
                ResourceType = resourceType,
                RetentionDays = retentionDays,
                ArchiveAfterDays = archiveAfterDays,
                EnableCompaction = enableCompaction,
                IsActive = true,
                NextExecutionAt = DateTime.UtcNow.AddDays(1)
            };

            // Set TenantId using SetProperties (EntityBase has protected setter)
            if (tenantId.HasValue) { existingPolicy.SetProperties(new Dictionary<string, object?> { ["TenantId"] = new TenantId(tenantId.Value) }); }

            existingPolicy = await policyRepository.CreateAsync(existingPolicy, cancellationToken);
        }

        logger.LogInformation("Set retention policy: TenantId={TenantId}, Type={Type}, Retention={Retention}days, Archive={Archive}days", tenantId, resourceType, retentionDays, archiveAfterDays);

        return existingPolicy;
    }

    public async Task<UsageRetentionPolicy?> GetPolicyAsync(Guid? tenantId, ResourceUsageType? resourceType, CancellationToken cancellationToken = default)
    {
        return await policyRepository.GetByTenantAndTypeAsync(tenantId, resourceType, cancellationToken);
    }

    public async Task<IEnumerable<UsageRetentionPolicy>> GetActivePoliciesAsync(CancellationToken cancellationToken = default) { return await policyRepository.GetActivePoliciesAsync(cancellationToken); }

    public async Task<RetentionExecutionResult> ExecuteRetentionAsync(Guid policyId, CancellationToken cancellationToken = default)
    {
        var policy = await policyRepository.GetByIdAsync(policyId, cancellationToken);

        if (policy is not { IsActive: true })
        {
            logger.LogWarning("Cannot execute retention for policy {PolicyId} - not found or inactive", policyId);

            return new RetentionExecutionResult();
        }

        logger.LogInformation("Executing retention policy {PolicyId}: {Name}", policyId, policy.Name);

        var result = new RetentionExecutionResult();

        try
        {
            // Archive old records
            var archiveThreshold = policy.GetArchiveThresholdDate();
            result.RecordsArchived = await usageRepository.ArchiveOlderThanAsync(archiveThreshold, cancellationToken);

            // Delete very old records
            var deleteThreshold = policy.GetDeletionThresholdDate();
            var deleted = await usageRepository.DeleteOlderThanAsync(deleteThreshold, cancellationToken);
            result.RecordsDeleted = deleted ? 1 : 0;

            // Compact records if enabled
            if (policy.EnableCompaction) { result.RecordsCompacted = await CompactUsageRecordsAsync(policy.TenantId ?? Guid.Empty, policy.ResourceType, archiveThreshold, cancellationToken); }

            // Update policy execution time
            policy.LastExecutedAt = DateTime.UtcNow;
            policy.NextExecutionAt = policy.CalculateNextCompaction();
            await policyRepository.UpdateAsync(policy, cancellationToken);

            logger.LogInformation("Retention policy {PolicyId} executed: Archived={Archived}, Deleted={Deleted}, Compacted={Compacted}", policyId, result.RecordsArchived, result.RecordsDeleted, result.RecordsCompacted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing retention policy {PolicyId}", policyId);

            throw;
        }

        return result;
    }

    public async Task<int> CompactUsageRecordsAsync(Guid tenantId, ResourceUsageType? type = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        if (tenantId == Guid.Empty)
        {
            logger.LogWarning("Cannot compact records without tenant ID");

            return 0;
        }

        var threshold = olderThan ?? DateTime.UtcNow.AddDays(-30);

        // If type is not specified, get all types for the tenant
        IEnumerable<UsageRecord> records;

        if (type.HasValue) { records = await usageRepository.GetByTenantAsync(tenantId, type.Value, null, threshold, cancellationToken); }
        else
        {
            records = await usageRepository.GetByTenantAsync(tenantId, cancellationToken);
            records = records.Where(r => r.PeriodStart <= threshold);
        }

        var recordsList = records.ToList();

        if (recordsList.Count == 0) return 0;

        // Group by month and create aggregated records
        var monthlyGroups = recordsList.GroupBy(r => new { r.Type, r.PeriodStart.Year, r.PeriodStart.Month }).ToList();

        var compactedCount = 0;

        foreach (var group in monthlyGroups)
        {
            var periodStart = new DateTime(group.Key.Year, group.Key.Month, 1);
            var totalUsage = group.Sum(r => r.UsageAmount);

            // Create monthly aggregated record
            var monthlyRecord = UsageRecord.CreateMonthly(group.Key.Type, tenantId, totalUsage, periodStart);

            await usageRepository.AddAsync(monthlyRecord, cancellationToken);
            compactedCount++;
        }

        // Delete the original detailed records
        await usageRepository.DeleteOlderThanAsync(threshold, cancellationToken);

        logger.LogInformation("Compacted {Count} usage records into {MonthlyCount} monthly records for tenant {TenantId}", recordsList.Count, compactedCount, tenantId);

        return compactedCount;
    }

    public async Task<int> ArchiveUsageRecordsAsync(Guid tenantId, ResourceUsageType? type = null, DateTime? olderThan = null, CancellationToken cancellationToken = default)
    {
        var threshold = olderThan ?? DateTime.UtcNow.AddDays(-90);

        return await usageRepository.ArchiveOlderThanAsync(threshold, cancellationToken);
    }

    public async Task<int> DeleteArchivedRecordsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var deleted = await usageRepository.DeleteOlderThanAsync(olderThan, cancellationToken);

        return deleted ? 1 : 0;
    }

    public async Task<RetentionStats> GetRetentionStatsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // This would require additional repository methods to get aggregated stats
        // For now, return basic stats
        logger.LogWarning("GetRetentionStatsAsync not fully implemented - returning placeholder");

        return new RetentionStats { TotalRecords = 0, ArchivedRecords = 0, ActiveRecords = 0, TotalStorageBytes = 0, ArchivedStorageBytes = 0, OldestRecordDate = null };
    }

    // TODO: Integration with Storage module for cold storage management
    // TODO: Integration with Backup module for data archival
    // TODO: Implement GetRetentionStatsAsync with proper repository methods
}
