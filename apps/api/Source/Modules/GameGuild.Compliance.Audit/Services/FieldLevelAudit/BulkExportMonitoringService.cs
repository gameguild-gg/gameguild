using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

/// <summary>Configuration for field-level auditing features (issue #186).</summary>
public sealed class FieldLevelAuditOptions
{
    public const string SectionName = "Compliance:FieldLevelAudit";

    /// <summary>Maximum records a single export may return before it is flagged as bulk (default 1000).</summary>
    public int MaxRecordsPerExport { get; set; } = 1000;

    /// <summary>Maximum total records one user may export within the sliding window before alerting.</summary>
    public int MaxRecordsPerUserPerDay { get; set; } = 10000;

    /// <summary>When true, exports that exceed a threshold are rejected; when false they complete but are flagged and alerted.</summary>
    public bool BlockExceedingExports { get; set; }

    /// <summary>Home jurisdiction of the platform (ISO 3166-1 alpha-2). Transfers to other jurisdictions are validated.</summary>
    public string HomeJurisdiction { get; set; } = "BR";

    /// <summary>Jurisdictions the platform is allowed to transfer personal data to (ISO 3166-1 alpha-2).</summary>
    public string[] AllowedJurisdictions { get; set; } = ["BR", "US", "CA", "EU", "GB", "JP"];

    /// <summary>Jurisdictions that always require manual review before a transfer may proceed.</summary>
    public string[] ReviewJurisdictions { get; set; } = [];

    /// <summary>Default retention days applied to sensitive field-access records when no tenant policy exists.</summary>
    public int DefaultSensitiveRetentionDays { get; set; } = 365;

    /// <summary>Hour (UTC) marking the start of the off-hours window used by access-pattern analysis.</summary>
    public int OffHoursStartUtc { get; set; } = 20;

    /// <summary>Hour (UTC) marking the end of the off-hours window used by access-pattern analysis.</summary>
    public int OffHoursEndUtc { get; set; } = 6;

    /// <summary>
    /// When true (default), the universal entity-change pipeline stages one <c>FieldAccessAudit</c> row per
    /// changed field of every persisted entity write (issue #186 criterion 1), with automatic classification
    /// and privacy-safe masked values. Disable only to shed the per-field storage overhead.
    /// </summary>
    public bool CaptureEntityWrites { get; set; } = true;

    /// <summary>
    /// When true (default), endpoints annotated with <c>[DataMaskingResourceType]</c> record one field-level
    /// read observation per sensitive field of every entity they return, using the trusted actor context.
    /// </summary>
    public bool CaptureEndpointReads { get; set; } = true;

    /// <summary>Maximum response items the read-capture pass will walk per request (page-size guard).</summary>
    public int MaxReadCaptureItemsPerResponse { get; set; } = 100;
}

/// <summary>Outcome of evaluating one export attempt against the bulk-export thresholds.</summary>
public sealed record BulkExportEvaluation(
    BulkExportEvent Event,
    bool ThresholdExceeded,
    long DailyTotal,
    bool DailyThresholdExceeded,
    bool Blocked)
{
    public bool ShouldAlert => ThresholdExceeded || DailyThresholdExceeded;
}

public interface IBulkExportMonitoringService
{
    Task<BulkExportEvaluation> EvaluateAndRecordAsync(
        Guid tenantId,
        Guid? userId,
        BulkExportScope scope,
        long recordCount,
        string? ipAddress = null,
        string? correlationId = null,
        string? destination = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BulkExportEvent>> GetExportsAsync(
        Guid? tenantId, bool? thresholdExceeded, int skip, int take, CancellationToken cancellationToken = default);
}

/// <summary>
/// Bulk data export detection with threshold-based alerting (issue #186): every export attempt is
/// recorded with the threshold it was evaluated against; attempts exceeding the per-export record
/// cap or the per-user daily budget raise a durable security alert and, when configured, are blocked.
/// </summary>
public sealed class BulkExportMonitoringService(
    IApplicationDbContext context,
    IOptions<FieldLevelAuditOptions> options,
    TimeProvider timeProvider) : IBulkExportMonitoringService
{
    public async Task<BulkExportEvaluation> EvaluateAndRecordAsync(
        Guid tenantId,
        Guid? userId,
        BulkExportScope scope,
        long recordCount,
        string? ipAddress = null,
        string? correlationId = null,
        string? destination = null,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var maxPerExport = options.Value.MaxRecordsPerExport;

        var dailyTotal = recordCount;
        if (userId.HasValue)
        {
            var dayStart = now.Date;
            var prior = await context.Set<BulkExportEvent>()
                .Where(export => export.TenantId == tenantId
                    && export.UserId == userId
                    && export.OccurredAtUtc >= dayStart)
                .SumAsync(export => export.RecordCount, cancellationToken).ConfigureAwait(false);
            dailyTotal = prior + recordCount;
        }

        var exportExceeded = recordCount > maxPerExport;
        var dailyExceeded = dailyTotal > options.Value.MaxRecordsPerUserPerDay;
        var exportEvent = BulkExportEvent.Record(
            tenantId, userId, scope, recordCount, maxPerExport, exportExceeded, now, ipAddress, correlationId, destination);
        context.Set<BulkExportEvent>().Add(exportEvent);

        if (exportExceeded || dailyExceeded)
        {
            var alert = SecurityAlert.Raise(
                tenantId,
                SecurityAlertRules.BulkExportThresholdExceeded,
                SecurityEventKind.DataMovement,
                AuditRiskLevel.High,
                "Bulk export threshold exceeded",
                exportExceeded
                    ? $"Export of {recordCount} records exceeded the {maxPerExport}-record threshold"
                    : $"Daily export volume {dailyTotal} exceeded the {options.Value.MaxRecordsPerUserPerDay}-record budget",
                AuditActionTypes.BulkExportThresholdExceeded,
                sourceAuditLogId: null,
                subjectUserId: userId,
                ipAddress: ipAddress,
                detectedAtUtc: now);
            context.Set<SecurityAlert>().Add(alert);
            exportEvent.MarkAlertRaised(now);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new BulkExportEvaluation(exportEvent, exportExceeded, dailyTotal, dailyExceeded, options.Value.BlockExceedingExports && exportExceeded);
    }

    public async Task<IReadOnlyList<BulkExportEvent>> GetExportsAsync(
        Guid? tenantId, bool? thresholdExceeded, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<BulkExportEvent>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(export => export.TenantId == tenantId.Value);
        }
        if (thresholdExceeded.HasValue)
        {
            query = query.Where(export => export.ThresholdExceeded == thresholdExceeded.Value);
        }

        return await query
            .OrderByDescending(export => export.OccurredAtUtc)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
