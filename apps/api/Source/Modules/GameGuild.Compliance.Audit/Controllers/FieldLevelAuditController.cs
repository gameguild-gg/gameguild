using Asp.Versioning;
using GameGuild.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using GameGuild.Configuration.PresentationLayer.RateLimiting;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Field-level data access auditing with sensitive field masking (issue #186): field read/write
/// tracking, automatic sensitive-field classification, access-pattern anomaly detection, bulk export
/// monitoring, the GDPR Article 30 processing register, data subject access monitoring, cross-border
/// transfer validation and sensitive-data retention lifecycle enforcement.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/audit/field-level")]
[Route("api/audit/field-level")]
[Tags("compliance/audit/field-level")]
[Authorize]
[EnableRateLimiting(RateLimitPolicies.Api)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class FieldLevelAuditController(
    FieldAccessAuditService fieldAccessAudit,
    IFieldAccessPatternAnalysisService patternAnalysis,
    IBulkExportMonitoringService bulkExports,
    IGdprArticle30Service article30,
    ICrossBorderTransferService crossBorder,
    ISensitiveDataRetentionService sensitiveRetention,
    IDataSubjectAccessMonitoringService subjectMonitoring,
    IActorContextAccessor actors,
    ISender sender) : ControllerBase
{
    // ── Field-level access tracking (criterion 1) ────────────────────────────

    /// <summary>Field access history for one entity (or one field of it), newest first.</summary>
    [HttpGet("access-history/{entityId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<FieldAccessAuditResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FieldAccessAuditResponse>>> GetAccessHistory(
        Guid entityId, [FromQuery] string? fieldName, CancellationToken cancellationToken)
    {
        var history = await fieldAccessAudit.GetFieldAccessHistoryAsync(entityId, fieldName, cancellationToken).ConfigureAwait(false);
        return Ok(history.IsSuccess
            ? history.Value.Select(FieldAccessAuditResponse.From).ToList()
            : Problem(title: history.Error.Description, statusCode: StatusCodes.Status400BadRequest));
    }

    /// <summary>Records a field-level access observation (read or write) with automatic classification and masking.</summary>
    [HttpPost("access")]
    [ProducesResponseType(typeof(FieldAccessAuditResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public Task<ActionResult<FieldAccessAuditResponse>> RecordAccess(
        [FromBody] RecordFieldAccessRequest request, CancellationToken cancellationToken) =>
        Execute<FieldAccessAuditResponse>(async () =>
            await sender.Send(new RecordFieldAccessCommand(request), cancellationToken).ConfigureAwait(false));

    // ── Classification (criterion 2) ─────────────────────────────────────────

    /// <summary>The sensitive-field classification registry.</summary>
    [HttpGet("classifications")]
    [ProducesResponseType(typeof(IReadOnlyList<SensitiveFieldClassificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SensitiveFieldClassificationResponse>>> GetClassifications(
        CancellationToken cancellationToken)
    {
        var classifications = await fieldAccessAudit.GetClassificationsAsync(actors.ActorContext.TenantId, cancellationToken).ConfigureAwait(false);
        return Ok(classifications.Select(SensitiveFieldClassificationResponse.From).ToList());
    }

    /// <summary>Re-scans recent field access observations and upserts automatic classifications.</summary>
    [HttpPost("classifications/scan")]
    [ProducesResponseType(typeof(SensitiveFieldScanResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<SensitiveFieldScanResponse>> ScanClassifications(CancellationToken cancellationToken) =>
        Execute<SensitiveFieldScanResponse>(async () =>
            await sender.Send(new ScanSensitiveFieldsCommand(actors.ActorContext.TenantId), cancellationToken).ConfigureAwait(false));

    // ── Access-pattern analysis (criterion 3) ────────────────────────────────

    /// <summary>Anomaly findings from access-pattern analysis, optionally filtered by status.</summary>
    [HttpGet("anomalies")]
    [ProducesResponseType(typeof(IReadOnlyList<FieldAccessAnomalyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FieldAccessAnomalyResponse>>> GetAnomalies(
        [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        FieldAccessAnomalyStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status)
            && !Enum.TryParse<FieldAccessAnomalyStatus>(status, ignoreCase: true, out parsed))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Errors = { ["status"] = [$"Unknown anomaly status '{status}'."] }
            });
        }

        var anomalies = await patternAnalysis.GetAnomaliesAsync(actors.ActorContext.TenantId, parsed, skip, take, cancellationToken).ConfigureAwait(false);
        return Ok(anomalies.Select(FieldAccessAnomalyResponse.From).ToList());
    }

    /// <summary>Runs access-pattern analysis over the recent window and persists detected anomalies.</summary>
    [HttpPost("anomalies/analyze")]
    [ProducesResponseType(typeof(IReadOnlyList<FieldAccessPatternAnalysisResult>), StatusCodes.Status200OK)]
    public Task<ActionResult<IReadOnlyList<FieldAccessPatternAnalysisResult>>> AnalyzePatterns(
        [FromBody] AnalyzeFieldAccessPatternsRequest? request, CancellationToken cancellationToken) =>
        Execute<IReadOnlyList<FieldAccessPatternAnalysisResult>>(async () =>
            await sender.Send(
                new AnalyzeFieldAccessPatternsCommand(request ?? new AnalyzeFieldAccessPatternsRequest(null, null)),
                cancellationToken).ConfigureAwait(false));

    /// <summary>Acknowledges an anomaly finding (investigation started).</summary>
    [HttpPost("anomalies/{id:guid}/acknowledge")]
    [ProducesResponseType(typeof(FieldAccessAnomalyResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<FieldAccessAnomalyResponse>> AcknowledgeAnomaly(Guid id, CancellationToken cancellationToken) =>
        Execute<FieldAccessAnomalyResponse>(async () =>
            await sender.Send(new AcknowledgeFieldAccessAnomalyCommand(id), cancellationToken).ConfigureAwait(false));

    /// <summary>Resolves an anomaly finding with notes.</summary>
    [HttpPost("anomalies/{id:guid}/resolve")]
    [ProducesResponseType(typeof(FieldAccessAnomalyResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<FieldAccessAnomalyResponse>> ResolveAnomaly(
        [FromBody] ResolveFieldAccessAnomalyRequest request, CancellationToken cancellationToken) =>
        Execute<FieldAccessAnomalyResponse>(async () =>
            await sender.Send(new ResolveFieldAccessAnomalyCommand(request), cancellationToken).ConfigureAwait(false));

    // ── Bulk export detection (criterion 4) ──────────────────────────────────

    /// <summary>Observed bulk exports, optionally only threshold-exceeding ones.</summary>
    [HttpGet("bulk-exports")]
    [ProducesResponseType(typeof(IReadOnlyList<BulkExportEvent>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BulkExportEvent>>> GetBulkExports(
        [FromQuery] bool? thresholdExceeded, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        var exports = await bulkExports.GetExportsAsync(actors.ActorContext.TenantId, thresholdExceeded, skip, take, cancellationToken).ConfigureAwait(false);
        return Ok(exports);
    }

    /// <summary>Evaluates an export attempt against the bulk-export thresholds, records it and alerts when exceeded.</summary>
    [HttpPost("bulk-exports/evaluate")]
    [ProducesResponseType(typeof(BulkExportEvaluationResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<BulkExportEvaluationResponse>> EvaluateBulkExport(
        [FromBody] EvaluateBulkExportRequest request, CancellationToken cancellationToken) =>
        Execute<BulkExportEvaluationResponse>(async () =>
            await sender.Send(new EvaluateBulkExportCommand(request), cancellationToken).ConfigureAwait(false));

    // ── GDPR Article 30 register (criterion 7) ───────────────────────────────

    /// <summary>The GDPR Article 30 processing activity register, with completeness gaps listed.</summary>
    [HttpGet("gdpr/article-30")]
    [ProducesResponseType(typeof(GdprArticle30Response), StatusCodes.Status200OK)]
    public async Task<ActionResult<GdprArticle30Response>> GetArticle30Register(CancellationToken cancellationToken)
    {
        var tenantId = actors.ActorContext.TenantId;
        var activities = await article30.GetActivitiesAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var gaps = await article30.GetCompletenessGapsAsync(tenantId, cancellationToken).ConfigureAwait(false);
        return Ok(new GdprArticle30Response(
            activities.Select(GdprProcessingActivityResponse.From).ToList(),
            gaps));
    }

    /// <summary>Creates or updates a processing activity record.</summary>
    [HttpPut("gdpr/article-30")]
    [ProducesResponseType(typeof(GdprProcessingActivityResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<GdprProcessingActivityResponse>> SaveActivity(
        [FromBody] SaveGdprActivityRequest request, CancellationToken cancellationToken) =>
        Execute<GdprProcessingActivityResponse>(async () =>
            await sender.Send(new SaveGdprActivityCommand(request), cancellationToken).ConfigureAwait(false));

    /// <summary>Deletes a processing activity record.</summary>
    [HttpDelete("gdpr/article-30/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteActivity(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await sender.Send(new DeleteGdprActivityCommand(id), cancellationToken).ConfigureAwait(false);
        return deleted ? NoContent() : NotFound();
    }

    /// <summary>(Re)derives the platform processing activities for the tenant; manual entries are preserved.</summary>
    [HttpPost("gdpr/article-30/seed")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public Task<ActionResult<int>> SeedActivities(CancellationToken cancellationToken) =>
        Execute<int>(async () =>
            await sender.Send(new SeedGdprActivitiesCommand(actors.ActorContext.TenantId), cancellationToken).ConfigureAwait(false));

    /// <summary>Marks a processing activity reviewed by the current actor.</summary>
    [HttpPost("gdpr/article-30/{id:guid}/review")]
    [ProducesResponseType(typeof(GdprProcessingActivityResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<GdprProcessingActivityResponse>> MarkReviewed(Guid id, CancellationToken cancellationToken) =>
        Execute<GdprProcessingActivityResponse>(async () =>
            await sender.Send(new MarkGdprActivityReviewedCommand(id), cancellationToken).ConfigureAwait(false));

    // ── Cross-border transfers (criterion 9) ─────────────────────────────────

    /// <summary>Cross-border transfer log, optionally filtered by validation status.</summary>
    [HttpGet("cross-border-transfers")]
    [ProducesResponseType(typeof(IReadOnlyList<CrossBorderTransferLog>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CrossBorderTransferLog>>> GetTransfers(
        [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        CrossBorderTransferStatus? parsed = null;
        if (!string.IsNullOrWhiteSpace(status)
            && !Enum.TryParse<CrossBorderTransferStatus>(status, ignoreCase: true, out parsed))
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Errors = { ["status"] = [$"Unknown transfer status '{status}'."] }
            });
        }

        var transfers = await crossBorder.GetTransfersAsync(actors.ActorContext.TenantId, parsed, skip, take, cancellationToken).ConfigureAwait(false);
        return Ok(transfers);
    }

    /// <summary>Validates a prospective cross-border transfer against the jurisdiction policy and logs the outcome.</summary>
    [HttpPost("cross-border-transfers/validate")]
    [ProducesResponseType(typeof(CrossBorderTransferResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<CrossBorderTransferResponse>> ValidateTransfer(
        [FromBody] ValidateCrossBorderTransferRequest request, CancellationToken cancellationToken) =>
        Execute<CrossBorderTransferResponse>(async () =>
            await sender.Send(new ValidateCrossBorderTransferCommand(request), cancellationToken).ConfigureAwait(false));

    // ── Data subject access tracking with consent monitoring (criterion 8) ───

    /// <summary>Data subject rights requests with statutory deadline tracking, optionally filtered by status.</summary>
    [HttpGet("data-subject-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<DataSubjectRequestResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DataSubjectRequestResponse>>> GetDataSubjectRequests(
        [FromQuery] string? status, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        try
        {
            var requests = await subjectMonitoring.GetRequestsAsync(actors.ActorContext.TenantId, status, skip, take, cancellationToken).ConfigureAwait(false);
            return Ok(requests);
        }
        catch (FieldLevelAuditValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Errors = { [exception.Field] = [exception.Message] }
            });
        }
    }

    /// <summary>Consent monitoring for one data subject: consent states plus their rights requests.</summary>
    [HttpGet("data-subject-requests/subjects/{userId:guid}")]
    [ProducesResponseType(typeof(DataSubjectConsentMonitoringResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataSubjectConsentMonitoringResponse>> GetSubjectMonitoring(
        Guid userId, CancellationToken cancellationToken) =>
        Ok(await subjectMonitoring.GetSubjectMonitoringAsync(userId, cancellationToken).ConfigureAwait(false));

    /// <summary>Aggregate monitoring summary: open, fulfilled and overdue requests plus active consents.</summary>
    [HttpGet("data-subject-requests/summary")]
    [ProducesResponseType(typeof(DataSubjectAccessMonitoringSummary), StatusCodes.Status200OK)]
    public async Task<ActionResult<DataSubjectAccessMonitoringSummary>> GetMonitoringSummary(
        CancellationToken cancellationToken) =>
        Ok(await subjectMonitoring.GetSummaryAsync(actors.ActorContext.TenantId, cancellationToken).ConfigureAwait(false));

    // ── Retention lifecycle (criterion 10) ───────────────────────────────────

    /// <summary>Per-category sensitive-data retention rules.</summary>
    [HttpGet("retention")]
    [ProducesResponseType(typeof(IReadOnlyList<SensitiveDataRetentionPolicyResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SensitiveDataRetentionPolicyResponse>>> GetRetentionPolicies(
        CancellationToken cancellationToken)
    {
        var policies = await sensitiveRetention.GetPoliciesAsync(actors.ActorContext.TenantId, cancellationToken).ConfigureAwait(false);
        return Ok(policies.Select(SensitiveDataRetentionPolicyResponse.From).ToList());
    }

    /// <summary>Configures the retention rule for one sensitive data category.</summary>
    [HttpPut("retention")]
    [ProducesResponseType(typeof(SensitiveDataRetentionPolicyResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<SensitiveDataRetentionPolicyResponse>> ConfigureRetention(
        [FromBody] ConfigureSensitiveDataRetentionRequest request, CancellationToken cancellationToken) =>
        Execute<SensitiveDataRetentionPolicyResponse>(async () =>
            await sender.Send(new ConfigureSensitiveDataRetentionCommand(request), cancellationToken).ConfigureAwait(false));

    /// <summary>Enforces the sensitive-data retention policies (purge or mask expired records); dry-run reports without changing data.</summary>
    [HttpPost("retention/enforce")]
    [ProducesResponseType(typeof(SensitiveDataRetentionEnforcementResult), StatusCodes.Status200OK)]
    public Task<ActionResult<SensitiveDataRetentionEnforcementResult>> EnforceRetention(
        [FromQuery] bool dryRun = false, CancellationToken cancellationToken = default) =>
        Execute<SensitiveDataRetentionEnforcementResult>(async () =>
            await sender.Send(new EnforceSensitiveDataRetentionCommand(actors.ActorContext.TenantId, dryRun), cancellationToken).ConfigureAwait(false));

    private async Task<ActionResult<T>> Execute<T>(Func<Task<T>> operation) where T : class
    {
        try
        {
            var response = await operation().ConfigureAwait(false);
            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (FieldLevelAuditValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Errors = { [exception.Field] = [exception.Message] }
            });
        }
        catch (FieldLevelAuditExportBlockedException exception)
        {
            return Problem(title: exception.Message, statusCode: StatusCodes.Status413RequestEntityTooLarge);
        }
    }
}

/// <summary>The Article 30 register with its completeness gaps.</summary>
public sealed record GdprArticle30Response(
    IReadOnlyList<GdprProcessingActivityResponse> Activities,
    IReadOnlyList<string> CompletenessGaps);
