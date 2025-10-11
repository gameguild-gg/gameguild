using GameGuild.Database;
using GameGuild.Modules.Resources.Abstractions;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Events;
using GameGuild.Modules.Resources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Implementation of SLA impact analysis service
/// </summary>
public class SlaImpactAnalysisService : ISlaImpactAnalysisService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SlaImpactAnalysisService> _logger;

    public SlaImpactAnalysisService(
        ApplicationDbContext context,
        ILogger<SlaImpactAnalysisService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SlaImpactAnalysis> AnalyzeImpactAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Analyzing SLA impact for tenant {TenantId}, resource {ResourceType}",
            tenantId, resourceType);

        // Get resource usage data
        var usageRecords = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId &&
                        r.UsageType == resourceType &&
                        r.RecordedAt >= periodStart &&
                        r.RecordedAt < periodEnd)
            .ToListAsync(cancellationToken);

        // Get throttling events
        var quota = await _context.Set<ResourceQuota>()
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.UsageType == resourceType, cancellationToken);

        var throttlingEvents = usageRecords.Count(r => r.Count > (quota?.HardLimit ?? long.MaxValue));

        // Calculate mock performance metrics (in real implementation, would query performance monitoring system)
        var avgResponseTime = CalculateMockResponseTime(usageRecords, quota);
        var p95ResponseTime = avgResponseTime * 1.5;
        var p99ResponseTime = avgResponseTime * 2.0;
        var errorRate = throttlingEvents > 0 ? (throttlingEvents / (double)usageRecords.Count) * 100 : 0;
        var utilization = quota != null && quota.HardLimit > 0
            ? (quota.CurrentUsage / (double)quota.HardLimit) * 100
            : 0;

        var slaTarget = 1000.0; // 1 second default
        var slaViolations = usageRecords.Count(r => avgResponseTime > slaTarget);
        var slaCompliance = usageRecords.Count > 0
            ? ((usageRecords.Count - slaViolations) / (double)usageRecords.Count) * 100
            : 100;

        var impactSeverity = DetermineImpactSeverity(slaCompliance, errorRate, throttlingEvents);

        var analysis = new SlaImpactAnalysis
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ResourceType = resourceType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            AverageResponseTime = avgResponseTime,
            P95ResponseTime = p95ResponseTime,
            P99ResponseTime = p99ResponseTime,
            ErrorRate = errorRate,
            ResourceUtilization = utilization,
            ThrottlingEvents = throttlingEvents,
            SlaViolations = slaViolations,
            SlaTarget = slaTarget,
            SlaCompliance = slaCompliance,
            ImpactSeverity = impactSeverity,
            RootCause = GenerateRootCause(utilization, throttlingEvents, errorRate),
            RecommendedActions = JsonSerializer.Serialize(GenerateRecommendations(utilization, throttlingEvents)),
            IsComplete = true,
            CompletedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Set<SlaImpactAnalysis>().Add(analysis);
        await _context.SaveChangesAsync(cancellationToken);

        return analysis;
    }

    public async Task<List<SlaImpactAnalysis>> GetAnalysisByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Set<SlaImpactAnalysis>()
            .Where(a => a.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(a => a.PeriodStart >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(a => a.PeriodEnd <= endDate.Value);

        return await query.OrderByDescending(a => a.PeriodStart).ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<ResourceUsageType, int>> GetViolationsSummaryAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<SlaImpactAnalysis>()
            .Where(a => a.TenantId == tenantId &&
                        a.PeriodStart >= periodStart &&
                        a.PeriodEnd <= periodEnd)
            .GroupBy(a => a.ResourceType)
            .Select(g => new { ResourceType = g.Key, Violations = g.Sum(a => a.SlaViolations) })
            .ToDictionaryAsync(x => x.ResourceType, x => x.Violations, cancellationToken);
    }

    public Task<double> CalculateUtilizationCorrelationAsync(
        Guid tenantId,
        ResourceUsageType resourceType,
        CancellationToken cancellationToken = default)
    {
        // Mock correlation coefficient (in real implementation, would use statistical analysis)
        return Task.FromResult(0.85); // Strong positive correlation
    }

    private double CalculateMockResponseTime(List<ResourceUsageRecord> records, ResourceQuota? quota)
    {
        if (quota == null || quota.HardLimit == 0) return 100.0;

        var utilization = quota.CurrentUsage / (double)quota.HardLimit;

        // Response time increases exponentially as utilization approaches limit
        return 100 + (utilization * utilization * 1000);
    }

    private string DetermineImpactSeverity(double slaCompliance, double errorRate, int throttlingEvents)
    {
        if (slaCompliance < 95 || errorRate > 5 || throttlingEvents > 100)
            return "Critical";
        if (slaCompliance < 98 || errorRate > 2 || throttlingEvents > 50)
            return "High";
        if (slaCompliance < 99.5 || errorRate > 1 || throttlingEvents > 10)
            return "Medium";
        return "Low";
    }

    private string GenerateRootCause(double utilization, int throttlingEvents, double errorRate)
    {
        if (utilization > 90 && throttlingEvents > 0)
            return "Resource utilization exceeded 90% causing throttling and performance degradation";
        if (throttlingEvents > 0)
            return $"Resource limits triggered {throttlingEvents} throttling events";
        if (errorRate > 1)
            return $"Elevated error rate of {errorRate:F2}% detected";
        return "No significant performance impact detected";
    }

    private List<string> GenerateRecommendations(double utilization, int throttlingEvents)
    {
        var recommendations = new List<string>();

        if (utilization > 80)
            recommendations.Add("Consider increasing resource limits to accommodate growth");
        if (throttlingEvents > 0)
            recommendations.Add("Review throttling policies and soft limits");
        if (utilization > 90)
            recommendations.Add("Implement auto-scaling or reserved capacity");

        if (recommendations.Count == 0)
            recommendations.Add("Current resource allocation is adequate");

        return recommendations;
    }
}

/// <summary>
/// Implementation of usage event streaming service
/// </summary>
public class UsageEventStreamService : IUsageEventStreamService
{
    private readonly ILogger<UsageEventStreamService> _logger;
    private readonly Dictionary<string, long> _stats = new();

    public UsageEventStreamService(ILogger<UsageEventStreamService> logger)
    {
        _logger = logger;
        _stats["EventsStreamed"] = 0;
        _stats["ErrorCount"] = 0;
    }

    public Task StreamEventAsync(UsageRecordedEvent usageEvent, CancellationToken cancellationToken = default)
    {
        // In real implementation, this would stream to Kafka/EventHub/etc.
        _logger.LogInformation("Streaming usage event {RecordId} for tenant {TenantId}",
            usageEvent.RecordId, usageEvent.TenantId);

        _stats["EventsStreamed"]++;
        return Task.CompletedTask;
    }

    public async Task StreamEventsAsync(List<UsageRecordedEvent> events, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Batch streaming {Count} usage events", events.Count);

        foreach (var evt in events)
        {
            await StreamEventAsync(evt, cancellationToken);
        }
    }

    public Task ConfigureStreamAsync(string streamType, Dictionary<string, string> configuration, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Configuring stream type {StreamType}", streamType);
        // In real implementation, configure Kafka/EventHub connection
        return Task.CompletedTask;
    }

    public Task<Dictionary<string, long>> GetStreamingStatsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_stats);
    }
}
