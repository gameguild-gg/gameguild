using Microsoft.EntityFrameworkCore;

namespace GameGuild.Compliance.Audit;

/// <summary>Result of one access-pattern analysis run for one user window.</summary>
public sealed record FieldAccessPatternAnalysisResult(
    Guid TenantId,
    Guid UserId,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc,
    long ObservedAccesses,
    long BaselineAccesses,
    int DistinctEntities,
    double SensitiveReadRatio,
    double OffHoursRatio,
    IReadOnlyList<FieldAccessAnomaly> Anomalies)
{
    public bool HasAnomalies => Anomalies.Count > 0;
}

public interface IFieldAccessPatternAnalysisService
{
    Task<FieldAccessPatternAnalysisResult> AnalyzeUserAsync(
        Guid tenantId,
        Guid userId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime baselineStartUtc,
        DateTime baselineEndUtc,
        int offHoursStartHour,
        int offHoursEndHour,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FieldAccessPatternAnalysisResult>> AnalyzeTenantAsync(
        Guid tenantId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime baselineStartUtc,
        DateTime baselineEndUtc,
        int offHoursStartHour,
        int offHoursEndHour,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FieldAccessAnomaly>> GetAnomaliesAsync(
        Guid? tenantId,
        FieldAccessAnomalyStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<FieldAccessAnomaly?> AcknowledgeAnomalyAsync(Guid anomalyId, Guid acknowledgedByUserId, CancellationToken cancellationToken = default);

    Task<FieldAccessAnomaly?> ResolveAnomalyAsync(Guid anomalyId, string? notes, CancellationToken cancellationToken = default);
}

/// <summary>
/// Access-pattern analysis (issue #186): compares one analysis window of field access observations
/// against a preceding baseline window per user and flags unusual behaviour — unusual volume, access
/// to an unusual number of distinct entities, an elevated ratio of sensitive-field reads, and access
/// concentrated in off-hours. Detected anomalies persist with their evidence so investigations can
/// reconstruct both the observed and baseline metrics.
/// </summary>
public sealed class FieldAccessPatternAnalysisService(
    IApplicationDbContext context,
    TimeProvider timeProvider) : IFieldAccessPatternAnalysisService
{
    /// <summary>A user's access volume must exceed baseline by this multiple before it is flagged.</summary>
    public const double VolumeMultipleThreshold = 3;

    /// <summary>Minimum baseline accesses before volume multiples are trusted; below this only absolute volume applies.</summary>
    public const int MinimumBaselineForRatio = 5;

    /// <summary>Distinct entities accessed in the window above which the access pattern is unusual.</summary>
    public const int DistinctEntityThreshold = 50;

    /// <summary>Share of sensitive-field reads above which the pattern is flagged.</summary>
    public const double SensitiveRatioThreshold = 0.6;

    /// <summary>Share of off-hours accesses above which the pattern is flagged.</summary>
    public const double OffHoursRatioThreshold = 0.5;

    /// <summary>Absolute access volume in one window that is always worth reviewing.</summary>
    public const long AbsoluteVolumeThreshold = 500;

    public async Task<FieldAccessPatternAnalysisResult> AnalyzeUserAsync(
        Guid tenantId,
        Guid userId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime baselineStartUtc,
        DateTime baselineEndUtc,
        int offHoursStartHour,
        int offHoursEndHour,
        CancellationToken cancellationToken = default)
    {
        var windowAccesses = await LoadAccessesAsync(tenantId, userId, windowStartUtc, windowEndUtc, cancellationToken).ConfigureAwait(false);
        var baselineAccesses = await LoadAccessesAsync(tenantId, userId, baselineStartUtc, baselineEndUtc, cancellationToken).ConfigureAwait(false);
        return AnalyzeLoaded(
            tenantId, userId, windowStartUtc, windowEndUtc, windowAccesses, baselineAccesses,
            offHoursStartHour, offHoursEndHour, persist: true, cancellationToken);
    }

    public async Task<IReadOnlyList<FieldAccessPatternAnalysisResult>> AnalyzeTenantAsync(
        Guid tenantId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        DateTime baselineStartUtc,
        DateTime baselineEndUtc,
        int offHoursStartHour,
        int offHoursEndHour,
        CancellationToken cancellationToken = default)
    {
        var windowByUser = (await context.Set<FieldAccessAudit>()
                .Where(access => access.TenantId == tenantId
                    && access.AccessedAt >= windowStartUtc
                    && access.AccessedAt <= windowEndUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .GroupBy(access => access.UserId)
            .ToDictionary(group => group.Key, group => group.ToList(), EqualityComparer<Guid>.Default);

        var baselineByUser = (await context.Set<FieldAccessAudit>()
                .Where(access => access.TenantId == tenantId
                    && access.AccessedAt >= baselineStartUtc
                    && access.AccessedAt <= baselineEndUtc)
                .ToListAsync(cancellationToken).ConfigureAwait(false))
            .GroupBy(access => access.UserId)
            .ToDictionary(group => group.Key, group => group.ToList(), EqualityComparer<Guid>.Default);

        var results = new List<FieldAccessPatternAnalysisResult>();
        foreach (var (userId, windowAccesses) in windowByUser)
        {
            baselineByUser.TryGetValue(userId, out var baselineAccesses);
            results.Add(AnalyzeLoaded(
                tenantId, userId, windowStartUtc, windowEndUtc, windowAccesses, baselineAccesses ?? [],
                offHoursStartHour, offHoursEndHour, persist: true, cancellationToken));
        }

        return results;
    }

    public async Task<IReadOnlyList<FieldAccessAnomaly>> GetAnomaliesAsync(
        Guid? tenantId, FieldAccessAnomalyStatus? status, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = context.Set<FieldAccessAnomaly>().AsQueryable();
        if (tenantId.HasValue)
        {
            query = query.Where(anomaly => anomaly.TenantId == tenantId.Value);
        }
        if (status.HasValue)
        {
            query = query.Where(anomaly => anomaly.Status == status.Value);
        }

        return await query
            .OrderByDescending(anomaly => anomaly.DetectedAtUtc)
            .Skip(Math.Max(0, skip))
            .Take(Math.Clamp(take, 1, 200))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<FieldAccessAnomaly?> AcknowledgeAnomalyAsync(
        Guid anomalyId, Guid acknowledgedByUserId, CancellationToken cancellationToken = default)
    {
        var anomaly = await context.Set<FieldAccessAnomaly>()
            .FirstOrDefaultAsync(candidate => candidate.Id == anomalyId, cancellationToken).ConfigureAwait(false);
        if (anomaly is null)
        {
            return null;
        }

        anomaly.Acknowledge(acknowledgedByUserId, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return anomaly;
    }

    public async Task<FieldAccessAnomaly?> ResolveAnomalyAsync(
        Guid anomalyId, string? notes, CancellationToken cancellationToken = default)
    {
        var anomaly = await context.Set<FieldAccessAnomaly>()
            .FirstOrDefaultAsync(candidate => candidate.Id == anomalyId, cancellationToken).ConfigureAwait(false);
        if (anomaly is null)
        {
            return null;
        }

        anomaly.Resolve(notes, timeProvider.GetUtcNow().UtcDateTime);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return anomaly;
    }

    private async Task<List<FieldAccessAudit>> LoadAccessesAsync(
        Guid tenantId, Guid userId, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken) =>
        await context.Set<FieldAccessAudit>()
            .Where(access => access.TenantId == tenantId
                && access.UserId == userId
                && access.AccessedAt >= fromUtc
                && access.AccessedAt <= toUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

    internal FieldAccessPatternAnalysisResult AnalyzeLoaded(
        Guid tenantId,
        Guid userId,
        DateTime windowStartUtc,
        DateTime windowEndUtc,
        IReadOnlyList<FieldAccessAudit> windowAccesses,
        IReadOnlyList<FieldAccessAudit> baselineAccesses,
        int offHoursStartHour,
        int offHoursEndHour,
        bool persist,
        CancellationToken cancellationToken)
    {
        var detectedAt = timeProvider.GetUtcNow().UtcDateTime;
        var anomalies = new List<FieldAccessAnomaly>();

        // Volume: multiple of baseline, or absolute floor for users without a meaningful baseline.
        if ((baselineAccesses.Count >= MinimumBaselineForRatio
                && windowAccesses.Count > 0
                && windowAccesses.Count >= baselineAccesses.Count * VolumeMultipleThreshold)
            || windowAccesses.Count >= AbsoluteVolumeThreshold)
        {
            anomalies.Add(FieldAccessAnomaly.Detect(
                tenantId, userId, FieldAccessAnomalyType.UnusualVolume,
                windowAccesses.Count, baselineAccesses.Count,
                Score(windowAccesses.Count, Math.Max(baselineAccesses.Count, 1)),
                $"User accessed {windowAccesses.Count} field values in the window against a baseline of {baselineAccesses.Count}.",
                windowStartUtc, windowEndUtc, detectedAt));
        }

        // Distinct entities: breadth of access beyond a plausible working set.
        var distinctEntities = windowAccesses
            .Select(access => access.EntityId)
            .Distinct()
            .Count();
        if (distinctEntities >= DistinctEntityThreshold)
        {
            anomalies.Add(FieldAccessAnomaly.Detect(
                tenantId, userId, FieldAccessAnomalyType.ExcessiveDistinctEntities,
                distinctEntities, DistinctEntityThreshold,
                Score(distinctEntities, DistinctEntityThreshold),
                $"User accessed {distinctEntities} distinct records in the window (threshold {DistinctEntityThreshold}).",
                windowStartUtc, windowEndUtc, detectedAt));
        }

        // Sensitive-read ratio.
        var sensitiveReads = windowAccesses.Count(access => access.IsSensitiveField);
        var sensitiveRatio = windowAccesses.Count == 0 ? 0 : (double)sensitiveReads / windowAccesses.Count;
        if (windowAccesses.Count >= MinimumBaselineForRatio && sensitiveRatio > SensitiveRatioThreshold)
        {
            anomalies.Add(FieldAccessAnomaly.Detect(
                tenantId, userId, FieldAccessAnomalyType.SensitiveReadRatio,
                sensitiveRatio, SensitiveRatioThreshold,
                Score(sensitiveRatio, SensitiveRatioThreshold),
                $"{sensitiveReads} of {windowAccesses.Count} accessed fields are classified sensitive ({sensitiveRatio:P0}).",
                windowStartUtc, windowEndUtc, detectedAt));
        }

        // Off-hours concentration.
        var offHours = windowAccesses.Count(access => IsOffHours(access.AccessedAt, offHoursStartHour, offHoursEndHour));
        var offHoursRatio = windowAccesses.Count == 0 ? 0 : (double)offHours / windowAccesses.Count;
        if (windowAccesses.Count >= MinimumBaselineForRatio && offHoursRatio > OffHoursRatioThreshold)
        {
            anomalies.Add(FieldAccessAnomaly.Detect(
                tenantId, userId, FieldAccessAnomalyType.OffHoursAccess,
                offHoursRatio, OffHoursRatioThreshold,
                Score(offHoursRatio, OffHoursRatioThreshold),
                $"{offHours} of {windowAccesses.Count} accesses occurred outside {offHoursStartHour:00}:00-{offHoursEndHour:00}:00 UTC.",
                windowStartUtc, windowEndUtc, detectedAt));
        }

        if (persist && anomalies.Count > 0)
        {
            context.Set<FieldAccessAnomaly>().AddRange(anomalies);
            // The analysis runs on the caller's context; persistence joins their transaction.
            _ = cancellationToken;
        }

        return new FieldAccessPatternAnalysisResult(
            tenantId, userId, windowStartUtc, windowEndUtc,
            windowAccesses.Count, baselineAccesses.Count, distinctEntities, sensitiveRatio, offHoursRatio, anomalies);
    }

    internal static bool IsOffHours(DateTime accessedAtUtc, int offHoursStartHour, int offHoursEndHour) =>
        offHoursStartHour <= offHoursEndHour
            ? accessedAtUtc.Hour >= offHoursStartHour && accessedAtUtc.Hour < offHoursEndHour
            : accessedAtUtc.Hour >= offHoursStartHour || accessedAtUtc.Hour < offHoursEndHour;

    private static double Score(double observed, double baseline) =>
        baseline <= 0 ? 1 : Math.Min(1, observed / (baseline * 2));
}
