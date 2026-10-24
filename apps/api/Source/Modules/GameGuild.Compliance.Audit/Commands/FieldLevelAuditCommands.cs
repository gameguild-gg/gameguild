using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Compliance.Audit;

// ── Commands (issue #186 field-level family) ─────────────────────────────────

public sealed record ScanSensitiveFieldsCommand(Guid? TenantId) : ICommand<SensitiveFieldScanResponse>;

public sealed record AnalyzeFieldAccessPatternsCommand(AnalyzeFieldAccessPatternsRequest Request) : ICommand<IReadOnlyList<FieldAccessPatternAnalysisResult>>;

public sealed record EvaluateBulkExportCommand(EvaluateBulkExportRequest Request) : ICommand<BulkExportEvaluationResponse>;

public sealed record SaveGdprActivityCommand(SaveGdprActivityRequest Request) : ICommand<GdprProcessingActivityResponse>;

public sealed record DeleteGdprActivityCommand(Guid Id) : ICommand<bool>;

public sealed record SeedGdprActivitiesCommand(Guid? TenantId) : ICommand<int>;

public sealed record MarkGdprActivityReviewedCommand(Guid Id) : ICommand<GdprProcessingActivityResponse>;

public sealed record ValidateCrossBorderTransferCommand(ValidateCrossBorderTransferRequest Request) : ICommand<CrossBorderTransferResponse>;

public sealed record ConfigureSensitiveDataRetentionCommand(ConfigureSensitiveDataRetentionRequest Request) : ICommand<SensitiveDataRetentionPolicyResponse>;

public sealed record EnforceSensitiveDataRetentionCommand(Guid? TenantId, bool DryRun) : ICommand<SensitiveDataRetentionEnforcementResult>;

public sealed record AcknowledgeFieldAccessAnomalyCommand(Guid Id) : ICommand<FieldAccessAnomalyResponse>;

public sealed record ResolveFieldAccessAnomalyCommand(ResolveFieldAccessAnomalyRequest Request) : ICommand<FieldAccessAnomalyResponse>;

public sealed record RecordFieldAccessCommand(RecordFieldAccessRequest Request) : ICommand<FieldAccessAuditResponse>;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed record SensitiveFieldScanResponse(int ScannedObservations, IReadOnlyList<SensitiveFieldClassificationResponse> Classifications);

public sealed record SensitiveFieldClassificationResponse(
    Guid Id,
    string EntityType,
    string FieldName,
    string Category,
    string Sensitivity,
    string DetectionRule,
    double Confidence,
    bool IsConfirmed,
    int ObservationCount,
    DateTime FirstSeenAt,
    DateTime LastSeenAt)
{
    public static SensitiveFieldClassificationResponse From(SensitiveFieldClassification classification) => new(
        classification.Id,
        classification.EntityType,
        classification.FieldName,
        classification.Category.ToString(),
        classification.Sensitivity.ToString(),
        classification.DetectionRule,
        classification.Confidence,
        classification.IsConfirmed,
        classification.ObservationCount,
        classification.FirstSeenAt,
        classification.LastSeenAt);
}

public sealed record AnalyzeFieldAccessPatternsRequest(
    Guid? UserId,
    Guid? TenantId,
    int WindowHours = 24,
    int BaselineHours = 168);

public sealed record ResolveFieldAccessAnomalyRequest(Guid Id, string? Notes);

public sealed record FieldAccessAnomalyResponse(
    Guid Id,
    Guid TenantId,
    Guid UserId,
    string AnomalyType,
    string Status,
    double ObservedValue,
    double BaselineValue,
    double Score,
    string Detail,
    DateTime WindowStartUtc,
    DateTime WindowEndUtc,
    DateTime DetectedAtUtc)
{
    public static FieldAccessAnomalyResponse From(FieldAccessAnomaly anomaly) => new(
        anomaly.Id,
        anomaly.TenantId,
        anomaly.UserId,
        anomaly.AnomalyType.ToString(),
        anomaly.Status.ToString(),
        anomaly.ObservedValue,
        anomaly.BaselineValue,
        anomaly.Score,
        anomaly.Detail,
        anomaly.WindowStartUtc,
        anomaly.WindowEndUtc,
        anomaly.DetectedAtUtc);
}

public sealed record EvaluateBulkExportRequest(
    BulkExportScope Scope,
    long RecordCount,
    string? IpAddress = null,
    string? CorrelationId = null,
    string? Destination = null);

public sealed record BulkExportEvaluationResponse(
    Guid EventId,
    long RecordCount,
    long Threshold,
    bool ThresholdExceeded,
    long DailyTotal,
    bool DailyThresholdExceeded,
    bool Blocked,
    bool AlertRaised);

public sealed record SaveGdprActivityRequest(
    Guid? Id,
    string ActivityName,
    string Purpose,
    string LegalBasis,
    string CategoriesOfDataSubjects,
    string CategoriesOfPersonalData,
    string Recipients,
    bool InvolvesCrossBorderTransfers,
    string? TransferDestinations,
    string RetentionPeriod,
    string SecurityMeasures);

public sealed record GdprProcessingActivityResponse(
    Guid Id,
    Guid? TenantId,
    string ActivityName,
    string Purpose,
    string LegalBasis,
    string CategoriesOfDataSubjects,
    string CategoriesOfPersonalData,
    string Recipients,
    bool InvolvesCrossBorderTransfers,
    string? TransferDestinations,
    string RetentionPeriod,
    string SecurityMeasures,
    bool IsAutoDerived,
    Guid? LastReviewedByUserId,
    DateTime? LastReviewedAtUtc)
{
    public static GdprProcessingActivityResponse From(GdprProcessingActivity activity) => new(
        activity.Id,
        activity.TenantId,
        activity.ActivityName,
        activity.Purpose,
        activity.LegalBasis,
        activity.CategoriesOfDataSubjects,
        activity.CategoriesOfPersonalData,
        activity.Recipients,
        activity.InvolvesCrossBorderTransfers,
        activity.TransferDestinations,
        activity.RetentionPeriod,
        activity.SecurityMeasures,
        activity.IsAutoDerived,
        activity.LastReviewedByUserId,
        activity.LastReviewedAtUtc);
}

public sealed record ValidateCrossBorderTransferRequest(
    string DestinationJurisdiction,
    string DataCategories,
    long RecordCount,
    string Purpose,
    string? CorrelationId = null);

public sealed record CrossBorderTransferResponse(
    Guid LogId,
    string DestinationJurisdiction,
    string Status,
    string Reason,
    bool IsCrossBorder,
    DateTime OccurredAtUtc);

public sealed record ConfigureSensitiveDataRetentionRequest(
    string Category,
    int RetentionDays,
    string Action);

public sealed record SensitiveDataRetentionPolicyResponse(
    Guid Id,
    Guid? TenantId,
    string Category,
    int RetentionDays,
    string Action,
    DateTime ConfiguredAtUtc,
    DateTime? LastEnforcedAtUtc,
    int LastEnforcedCount)
{
    public static SensitiveDataRetentionPolicyResponse From(SensitiveDataRetentionPolicy policy) => new(
        policy.Id,
        policy.TenantId,
        policy.Category.ToString(),
        policy.RetentionDays,
        policy.Action.ToString(),
        policy.ConfiguredAtUtc,
        policy.LastEnforcedAtUtc,
        policy.LastEnforcedCount);
}

public sealed record RecordFieldAccessRequest(
    string EntityType,
    Guid EntityId,
    FieldAccessType AccessType,
    IReadOnlyList<string> FieldNames,
    string? IpAddress = null,
    string? UserAgent = null);

public sealed record FieldAccessAuditResponse(
    Guid Id,
    Guid? TenantId,
    Guid UserId,
    string EntityType,
    Guid EntityId,
    string FieldName,
    string AccessType,
    bool IsSensitiveField,
    string SensitivityLevel,
    string? MaskedOldValue,
    string? MaskedNewValue,
    DateTime AccessedAt)
{
    public static FieldAccessAuditResponse From(FieldAccessAudit access) => new(
        access.Id,
        access.TenantId,
        access.UserId,
        access.EntityType,
        access.EntityId,
        access.FieldName,
        access.AccessType.ToString(),
        access.IsSensitiveField,
        access.SensitivityLevel.ToString(),
        access.MaskedOldValue,
        access.MaskedNewValue,
        access.AccessedAt);
}

// ── Handler ──────────────────────────────────────────────────────────────────

public sealed class FieldLevelAuditCommandHandler(
    IFieldAccessAuditService fieldAccessAudit,
    IFieldAccessPatternAnalysisService patternAnalysis,
    IBulkExportMonitoringService bulkExports,
    IGdprArticle30Service article30,
    ICrossBorderTransferService crossBorder,
    ISensitiveDataRetentionService sensitiveRetention,
    IActorContextAccessor actors,
    IOptions<FieldLevelAuditOptions> options,
    TimeProvider timeProvider,
    IApplicationDbContext context) :
    ICommandHandler<ScanSensitiveFieldsCommand, SensitiveFieldScanResponse>,
    ICommandHandler<AnalyzeFieldAccessPatternsCommand, IReadOnlyList<FieldAccessPatternAnalysisResult>>,
    ICommandHandler<EvaluateBulkExportCommand, BulkExportEvaluationResponse>,
    ICommandHandler<SaveGdprActivityCommand, GdprProcessingActivityResponse>,
    ICommandHandler<DeleteGdprActivityCommand, bool>,
    ICommandHandler<SeedGdprActivitiesCommand, int>,
    ICommandHandler<MarkGdprActivityReviewedCommand, GdprProcessingActivityResponse>,
    ICommandHandler<ValidateCrossBorderTransferCommand, CrossBorderTransferResponse>,
    ICommandHandler<ConfigureSensitiveDataRetentionCommand, SensitiveDataRetentionPolicyResponse>,
    ICommandHandler<EnforceSensitiveDataRetentionCommand, SensitiveDataRetentionEnforcementResult>,
    ICommandHandler<AcknowledgeFieldAccessAnomalyCommand, FieldAccessAnomalyResponse>,
    ICommandHandler<ResolveFieldAccessAnomalyCommand, FieldAccessAnomalyResponse>,
    ICommandHandler<RecordFieldAccessCommand, FieldAccessAuditResponse>
{
    public Task<SensitiveFieldScanResponse> Handle(ScanSensitiveFieldsCommand command, CancellationToken cancellationToken) =>
        ScanClassificationsAsync(command.TenantId ?? actors.ActorContext.TenantId, cancellationToken);

    public async Task<IReadOnlyList<FieldAccessPatternAnalysisResult>> Handle(AnalyzeFieldAccessPatternsCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        var tenantId = request.TenantId ?? actors.ActorContext.TenantId
            ?? throw new UnauthorizedAccessException("Field access pattern analysis requires a tenant context.");
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var windowStart = now.AddHours(-Math.Clamp(request.WindowHours, 1, 720));
        var windowEnd = now;
        var baselineStart = windowStart.AddHours(-Math.Clamp(request.BaselineHours, 1, 2160));
        var baselineEnd = windowStart;
        var settings = options.Value;

        if (request.UserId.HasValue)
        {
            var result = await patternAnalysis.AnalyzeUserAsync(
                tenantId, request.UserId.Value, windowStart, windowEnd, baselineStart, baselineEnd,
                settings.OffHoursStartUtc, settings.OffHoursEndUtc, cancellationToken).ConfigureAwait(false);
            return [result];
        }

        return await patternAnalysis.AnalyzeTenantAsync(
            tenantId, windowStart, windowEnd, baselineStart, baselineEnd,
            settings.OffHoursStartUtc, settings.OffHoursEndUtc, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BulkExportEvaluationResponse> Handle(EvaluateBulkExportCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request.RecordCount < 0)
        {
            throw new FieldLevelAuditValidationException("RecordCount", "Record count cannot be negative.");
        }

        var actor = actors.ActorContext;
        var tenantId = actor.TenantId ?? throw new UnauthorizedAccessException("Bulk export evaluation requires a tenant context.");
        var evaluation = await bulkExports.EvaluateAndRecordAsync(
            tenantId, actor.SubjectIdAsGuid, request.Scope, request.RecordCount,
            request.IpAddress ?? actor.TypedAttributes.IpAddress, request.CorrelationId, request.Destination, cancellationToken).ConfigureAwait(false);
        if (evaluation.Blocked)
        {
            throw new FieldLevelAuditExportBlockedException(evaluation.Event.RecordCount, evaluation.Event.Threshold);
        }

        return new BulkExportEvaluationResponse(
            evaluation.Event.Id, evaluation.Event.RecordCount, evaluation.Event.Threshold,
            evaluation.ThresholdExceeded, evaluation.DailyTotal, evaluation.DailyThresholdExceeded,
            evaluation.Blocked, evaluation.Event.AlertRaised);
    }

    public async Task<GdprProcessingActivityResponse> Handle(SaveGdprActivityCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        ValidateActivity(request);
        var tenantId = actors.ActorContext.TenantId;
        var existing = request.Id.HasValue ? await article30.GetActivityAsync(request.Id.Value, cancellationToken).ConfigureAwait(false) : null;
        if (request.Id.HasValue && existing is null)
        {
            throw new FieldLevelAuditValidationException("Id", "The processing activity does not exist.");
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var activity = existing ?? GdprProcessingActivity.Create(
            tenantId,
            request.ActivityName,
            request.Purpose,
            request.LegalBasis,
            request.CategoriesOfDataSubjects,
            request.CategoriesOfPersonalData,
            request.Recipients,
            request.InvolvesCrossBorderTransfers,
            request.TransferDestinations,
            request.RetentionPeriod,
            request.SecurityMeasures,
            isAutoDerived: false,
            createdAt: now);
        if (existing is not null)
        {
            activity.Update(
                request.Purpose, request.LegalBasis, request.CategoriesOfDataSubjects, request.CategoriesOfPersonalData,
                request.Recipients, request.InvolvesCrossBorderTransfers, request.TransferDestinations,
                request.RetentionPeriod, request.SecurityMeasures, now);
        }

        var saved = await article30.SaveActivityAsync(activity, cancellationToken).ConfigureAwait(false);
        return GdprProcessingActivityResponse.From(saved);
    }

    public async Task<bool> Handle(DeleteGdprActivityCommand command, CancellationToken cancellationToken) =>
        await article30.DeleteActivityAsync(command.Id, cancellationToken).ConfigureAwait(false);

    public async Task<int> Handle(SeedGdprActivitiesCommand command, CancellationToken cancellationToken) =>
        await article30.SeedPlatformActivitiesAsync(command.TenantId ?? actors.ActorContext.TenantId, cancellationToken).ConfigureAwait(false);

    public async Task<GdprProcessingActivityResponse> Handle(MarkGdprActivityReviewedCommand command, CancellationToken cancellationToken)
    {
        var reviewer = actors.ActorContext.SubjectIdAsGuid
            ?? throw new UnauthorizedAccessException("Reviewing a processing activity requires an authenticated actor.");
        var activity = await article30.MarkReviewedAsync(command.Id, reviewer, cancellationToken).ConfigureAwait(false)
            ?? throw new FieldLevelAuditValidationException("Id", "The processing activity does not exist.");
        return GdprProcessingActivityResponse.From(activity);
    }

    public async Task<CrossBorderTransferResponse> Handle(ValidateCrossBorderTransferCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (string.IsNullOrWhiteSpace(request.DestinationJurisdiction) || request.DestinationJurisdiction.Length != 2)
        {
            throw new FieldLevelAuditValidationException("DestinationJurisdiction", "Destination jurisdiction must be a two-letter ISO 3166-1 code.");
        }

        var actor = actors.ActorContext;
        var tenantId = actor.TenantId ?? throw new UnauthorizedAccessException("Cross-border transfer validation requires a tenant context.");
        var evaluation = await crossBorder.ValidateAndLogAsync(
            tenantId, actor.SubjectIdAsGuid, request.DestinationJurisdiction, request.DataCategories,
            request.RecordCount, request.Purpose, request.CorrelationId, cancellationToken).ConfigureAwait(false);
        return new CrossBorderTransferResponse(
            evaluation.Log.Id, evaluation.Log.DestinationJurisdiction, evaluation.Status.ToString(),
            evaluation.Reason, evaluation.IsCrossBorder, evaluation.Log.OccurredAtUtc);
    }

    public async Task<SensitiveDataRetentionPolicyResponse> Handle(ConfigureSensitiveDataRetentionCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (!Enum.TryParse<SensitiveDataCategory>(request.Category, ignoreCase: true, out var category) || category == SensitiveDataCategory.None)
        {
            throw new FieldLevelAuditValidationException("Category", $"Unknown sensitive data category '{request.Category}'.");
        }
        if (!Enum.TryParse<SensitiveDataRetentionAction>(request.Action, ignoreCase: true, out var action))
        {
            throw new FieldLevelAuditValidationException("Action", $"Unknown retention action '{request.Action}'.");
        }

        try
        {
            var policy = await sensitiveRetention.ConfigureAsync(
                actors.ActorContext.TenantId, category, request.RetentionDays, action,
                actors.ActorContext.SubjectIdAsGuid, cancellationToken).ConfigureAwait(false);
            return SensitiveDataRetentionPolicyResponse.From(policy);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FieldLevelAuditValidationException("RetentionDays", exception.Message);
        }
    }

    public async Task<SensitiveDataRetentionEnforcementResult> Handle(EnforceSensitiveDataRetentionCommand command, CancellationToken cancellationToken) =>
        await sensitiveRetention.EnforceAsync(
            command.TenantId ?? actors.ActorContext.TenantId,
            actors.ActorContext.SubjectIdAsGuid,
            command.DryRun,
            cancellationToken).ConfigureAwait(false);

    public async Task<FieldAccessAnomalyResponse> Handle(AcknowledgeFieldAccessAnomalyCommand command, CancellationToken cancellationToken)
    {
        var acknowledger = actors.ActorContext.SubjectIdAsGuid
            ?? throw new UnauthorizedAccessException("Acknowledging an anomaly requires an authenticated actor.");
        var anomaly = await patternAnalysis.AcknowledgeAnomalyAsync(command.Id, acknowledger, cancellationToken).ConfigureAwait(false)
            ?? throw new FieldLevelAuditValidationException("Id", "The anomaly does not exist.");
        return FieldAccessAnomalyResponse.From(anomaly);
    }

    public async Task<FieldAccessAnomalyResponse> Handle(ResolveFieldAccessAnomalyCommand command, CancellationToken cancellationToken)
    {
        var anomaly = await patternAnalysis.ResolveAnomalyAsync(command.Request.Id, command.Request.Notes, cancellationToken).ConfigureAwait(false)
            ?? throw new FieldLevelAuditValidationException("Id", "The anomaly does not exist.");
        return FieldAccessAnomalyResponse.From(anomaly);
    }

    public async Task<FieldAccessAuditResponse> Handle(RecordFieldAccessCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        if (request.FieldNames.Count == 0)
        {
            throw new FieldLevelAuditValidationException("FieldNames", "At least one field name is required.");
        }

        var actor = actors.ActorContext;
        var tenantId = actor.TenantId ?? throw new UnauthorizedAccessException("Field access recording requires a tenant context.");
        var userId = actor.SubjectIdAsGuid
            ?? throw new UnauthorizedAccessException("Field access recording requires an authenticated actor.");
        foreach (var fieldName in request.FieldNames)
        {
            var result = await fieldAccessAudit.RecordFieldAccessAsync(
                tenantId, userId, request.EntityType, request.EntityId, fieldName, request.AccessType,
                oldValue: null, newValue: null,
                isSensitive: false, sensitivityLevel: SensitivityLevel.Public,
                request.IpAddress ?? actor.TypedAttributes.IpAddress ?? string.Empty,
                request.UserAgent ?? actor.TypedAttributes.UserAgent ?? string.Empty,
                cancellationToken).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                return FieldAccessAuditResponse.From(result.Value);
            }
        }

        throw new InvalidOperationException("Field access recording produced no rows.");
    }

    private async Task<SensitiveFieldScanResponse> ScanClassificationsAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime.AddDays(-options.Value.DefaultSensitiveRetentionDays);
        var observations = await context.Set<FieldAccessAudit>()
            .Where(access => (tenantId == null || access.TenantId == tenantId) && access.AccessedAt >= cutoff)
            .OrderByDescending(access => access.AccessedAt)
            .Take(5000)
            .Select(access => new { access.EntityType, access.FieldName })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        var scanned = 0;
        foreach (var observation in observations)
        {
            var match = SensitiveFieldClassifier.Classify(observation.EntityType, observation.FieldName, null);
            if (match is { IsSensitive: true })
            {
                var now = timeProvider.GetUtcNow().UtcDateTime;
                var existing = await context.Set<SensitiveFieldClassification>()
                    .FirstOrDefaultAsync(classification =>
                        classification.TenantId == tenantId
                        && classification.EntityType == observation.EntityType
                        && classification.FieldName == observation.FieldName, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is null)
                {
                    context.Set<SensitiveFieldClassification>().Add(SensitiveFieldClassification.Create(
                        tenantId, observation.EntityType, observation.FieldName, match.Category, match.Sensitivity, match.Rule, match.Confidence, now));
                }
                else
                {
                    existing.ObserveAuto(match.Category, match.Sensitivity, match.Rule, match.Confidence, now);
                }
            }
            scanned++;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var classifications = await context.Set<SensitiveFieldClassification>()
            .Where(classification => tenantId == null || classification.TenantId == tenantId)
            .OrderBy(classification => classification.EntityType).ThenBy(classification => classification.FieldName)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        return new SensitiveFieldScanResponse(
            scanned,
            classifications.Select(SensitiveFieldClassificationResponse.From).ToList());
    }

    private static void ValidateActivity(SaveGdprActivityRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ActivityName))
        {
            throw new FieldLevelAuditValidationException("ActivityName", "Activity name is required.");
        }
        if (string.IsNullOrWhiteSpace(request.Purpose))
        {
            throw new FieldLevelAuditValidationException("Purpose", "Purpose of processing is required (Article 30(1)(a)).");
        }
        if (string.IsNullOrWhiteSpace(request.RetentionPeriod))
        {
            throw new FieldLevelAuditValidationException("RetentionPeriod", "Retention period is required (Article 30(1)(f)).");
        }
        if (string.IsNullOrWhiteSpace(request.SecurityMeasures))
        {
            throw new FieldLevelAuditValidationException("SecurityMeasures", "Security measures are required (Article 30(1)(g)).");
        }
    }
}

/// <summary>Validation error raised by field-level audit commands.</summary>
public sealed class FieldLevelAuditValidationException(string field, string message)
    : InvalidOperationException(message)
{
    public string Field { get; } = field;
}

/// <summary>Raised when an export is blocked because it exceeds the configured threshold and blocking is enabled.</summary>
public sealed class FieldLevelAuditExportBlockedException(long recordCount, long threshold)
    : InvalidOperationException($"Export of {recordCount} records was blocked: it exceeds the configured threshold of {threshold}.")
{
    public long RecordCount { get; } = recordCount;
    public long Threshold { get; } = threshold;
}
