using GameGuild.CQRS.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources;

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

    public Task<RetentionStats> GetRetentionStatsAsync(Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        // This would require additional repository methods to get aggregated stats
        // For now, return basic stats
        logger.LogWarning("GetRetentionStatsAsync not fully implemented - returning placeholder");

        return Task.FromResult(new RetentionStats { TotalRecords = 0, ArchivedRecords = 0, ActiveRecords = 0, TotalStorageBytes = 0, ArchivedStorageBytes = 0, OldestRecordDate = null });
    }

    // TODO: Integration with Storage module for cold storage management
    // TODO: Integration with Backup module for data archival
    // TODO: Implement GetRetentionStatsAsync with proper repository methods
    
    /* COLD STORAGE ARCHIVAL IMPLEMENTATION GUIDE
     * 
     * Current State: ArchiveUsageRecordsAsync sets IsArchived flag in database.
     * Production Enhancement: Move archived data to external blob storage (Azure Blob, S3, local filesystem).
     * 
     * IMPLEMENTATION APPROACH:
     * 
     * 1. STORAGE DESTINATION OPTIONS:
     *    - Azure Blob Storage (recommended for Azure deployments)
     *      - Use Azure.Storage.Blobs NuGet package
     *      - Configure BlobServiceClient with connection string or managed identity
     *      - Organize as: {tenant-id}/{year}/{month}/{resource-type}/usage-{date}.parquet
     *    
     *    - AWS S3 (for AWS deployments)
     *      - Use AWSSDK.S3 NuGet package
     *      - Use IAM roles for authentication
     *      - Similar partitioning scheme
     *    
     *    - Local filesystem (dev/testing or on-premises)
     *      - Use System.IO for file operations
     *      - Mount NAS/SAN storage for production
     * 
     * 2. ARCHIVAL FORMAT SELECTION:
     *    - Parquet (RECOMMENDED for analytics workloads)
     *      - Columnar format with excellent compression (~10x vs JSON)
     *      - Query-efficient for aggregations and filtering
     *      - Use Apache.Arrow or Parquet.NET library
     *      - Schema: TenantId, ResourceType, UsageAmount, PeriodStart, PeriodEnd, Metadata (JSON), ArchivedAt
     *    
     *    - CSV (for human readability and broad compatibility)
     *      - Simple text format, easy to import/export
     *      - Less efficient storage and querying
     *    
     *    - JSON Lines (jsonl) for semi-structured metadata
     *      - One JSON object per line for streaming processing
     *      - Good for preserving complex metadata
     * 
     * 3. ARCHIVAL WORKFLOW:
     *    a. Query records older than threshold (e.g., 90 days)
     *    b. Batch records by tenant/month/type (partitioning strategy)
     *    c. Serialize to chosen format (Parquet/CSV/JSON)
     *    d. Upload to blob storage with metadata tags:
     *       - tenant-id, resource-type, period-start, period-end, record-count, compressed-size
     *    e. Verify upload success (check ETag/MD5)
     *    f. Mark database records as archived with blob reference (BlobUri property)
     *    g. Optional: Delete from hot database after retention period (e.g., 30 days post-archive)
     * 
     * 4. RETRIEVAL PATTERN:
     *    - Implement GetArchivedUsageAsync(tenantId, dateRange) method
     *    - Check if data exists in hot storage (database)
     *    - If not found, query blob storage index/metadata
     *    - Download blob, deserialize, and return data
     *    - Cache frequently accessed archives in Redis/memory cache
     * 
     * 5. LIFECYCLE MANAGEMENT:
     *    - Configure blob storage lifecycle policies:
     *      - Move to Cool tier after 30 days (Azure)
     *      - Move to Archive tier after 180 days (Azure)
     *      - Transition to Glacier for S3 (AWS)
     *    - Set up automated deletion after legal retention period (e.g., 7 years)
     * 
     * 6. MONITORING & OBSERVABILITY:
     *    - Track archival metrics: records archived, bytes stored, compression ratio
     *    - Alert on archival failures or storage quota approaching limits
     *    - Log blob URIs for audit trail
     *    - Implement cost tracking for storage consumption
     * 
     * 7. EXAMPLE INTERFACE ADDITIONS:
     *    Task<ArchivalResult> ArchiveToColdStorageAsync(Guid tenantId, DateTime olderThan, CancellationToken ct);
     *    Task<IEnumerable<UsageRecord>> RetrieveFromColdStorageAsync(Guid tenantId, DateRange range, CancellationToken ct);
     *    Task<ColdStorageStats> GetColdStorageStatsAsync(Guid? tenantId, CancellationToken ct);
     * 
     * 8. INTEGRATION POINTS:
     *    - Add IArchiveStorageProvider abstraction with implementations:
     *      - AzureBlobArchiveProvider, S3ArchiveProvider, FileSystemArchiveProvider
     *    - Configure via appsettings.json:
     *      "ArchivalSettings": {
     *        "Provider": "AzureBlob",
     *        "ConnectionString": "...",
     *        "ContainerName": "usage-archives",
     *        "Format": "Parquet",
     *        "CompressionCodec": "Snappy"
     *      }
     *    - Register in DI container with factory pattern for multi-provider support
     * 
     * MIGRATION STRATEGY:
     * 1. Implement IArchiveStorageProvider + Parquet serialization
     * 2. Add background job to archive old records (Hangfire/Quartz)
     * 3. Run archival job once to migrate historical data
     * 4. Enable automated archival in retention policies
     * 5. Monitor storage costs and adjust retention thresholds
     */
}
