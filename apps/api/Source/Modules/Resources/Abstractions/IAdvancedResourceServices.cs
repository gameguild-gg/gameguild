namespace GameGuild.Modules.Resources.Abstractions;

/// <summary>
/// Service for SLA impact analysis and performance correlation
/// </summary>
public interface ISlaImpactAnalysisService
{
    /// <summary>
    /// Analyze SLA impact for a tenant and resource type
    /// </summary>
    Task<SlaImpactAnalysis> AnalyzeImpactAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA analysis reports by tenant
    /// </summary>
    Task<List<SlaImpactAnalysis>> GetAnalysisByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get SLA violations summary
    /// </summary>
    Task<Dictionary<ResourceUsageType, int>> GetViolationsSummaryAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate resource utilization correlation with performance
    /// </summary>
    Task<double> CalculateUtilizationCorrelationAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for usage retention and data lifecycle management
/// </summary>
public interface IUsageRetentionService
{
    /// <summary>
    /// Create or update retention policy
    /// </summary>
    Task<UsageRetentionPolicy> UpsertPolicyAsync(
        UsageRetentionPolicy policy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute retention policy
    /// </summary>
    Task<int> ExecutePolicyAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archive old usage records
    /// </summary>
    Task<int> ArchiveUsageRecordsAsync(
        Guid tenantId,
        DateTime olderThan,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Compact and down-sample usage data
    /// </summary>
    Task<int> CompactUsageDataAsync(
        Guid tenantId,
        ResourceUsageType? resourceType,
        string samplingStrategy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete expired usage records
    /// </summary>
    Task<int> DeleteExpiredRecordsAsync(
        Guid policyId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active retention policies
    /// </summary>
    Task<List<UsageRetentionPolicy>> GetActivePoliciesAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for reserved capacity and commitment discounts
/// </summary>
public interface IReservedCapacityService
{
    /// <summary>
    /// Create reserved capacity commitment
    /// </summary>
    Task<ReservedCapacity> CreateReservationAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long quantity,
        int commitmentMonths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Consume reserved capacity units
    /// </summary>
    Task<bool> ConsumeReservedUnitsAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long units,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active reservations for tenant
    /// </summary>
    Task<List<ReservedCapacity>> GetActiveReservationsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate pricing with reservation discount
    /// </summary>
    Task<decimal> CalculateDiscountedPriceAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        long units,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Renew expiring reservation
    /// </summary>
    Task<ReservedCapacity> RenewReservationAsync(
        Guid reservationId,
        int newCommitmentMonths,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get reservation utilization report
    /// </summary>
    Task<Dictionary<string, object>> GetUtilizationReportAsync(
        Guid reservationId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for real-time usage event streaming
/// </summary>
public interface IUsageEventStreamService
{
    /// <summary>
    /// Stream usage event to analytics platform
    /// </summary>
    Task StreamEventAsync(
        UsageRecordedEvent usageEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch stream multiple events
    /// </summary>
    Task StreamEventsAsync(
        List<UsageRecordedEvent> events,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Configure streaming destination (Kafka, EventHub, etc.)
    /// </summary>
    Task ConfigureStreamAsync(
        string streamType,
        Dictionary<string, string> configuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get streaming statistics
    /// </summary>
    Task<Dictionary<string, long>> GetStreamingStatsAsync(
        CancellationToken cancellationToken = default);
}
