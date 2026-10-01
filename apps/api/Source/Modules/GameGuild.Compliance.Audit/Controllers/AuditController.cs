using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Asp.Versioning;
using GameGuild.Configuration.PresentationLayer.RateLimiting;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using GameGuild.CQRS;

namespace GameGuild.Compliance.Audit;

/// <summary>
/// Controller for audit log management (admin only)
/// </summary>
[Microsoft.AspNetCore.Http.Tags("compliance/audit")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin/audit-logs")]
[Authorize(Policy = Policies.SystemAdmin)]
public class AuditController(
    IAuditService auditService,
    IActorContextAccessor actorContextAccessor,
    ILogger<AuditController> _logger,
    ISender sender,
    IAuditExportProgressTracker exportProgressTracker) : BaseApiController
{
    /// <summary>
    /// Gets the current user ID from the actor context
    /// </summary>
    protected Guid? GetCurrentUserId()
    {
        return actorContextAccessor.ActorContext.SubjectIdAsGuid;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<AuditLogResponse>> GetAuditLogs([FromQuery] AuditLogQueryRequest request)
    {
        var adminUserId = GetCurrentUserId();
        if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

        _logger.LogInformation("Admin {AdminUserId} querying audit logs: ActionType={ActionType}, RiskLevel={RiskLevel}", adminUserId.Value, request.ActionType, request.RiskLevel);

        // Log admin access to audit logs
        await auditService.LogAdminActionAsync(adminUserId.Value, "ViewAuditLogs", "Admin accessed audit logs", new { Filters = request, RequestedBy = adminUserId.Value }).ConfigureAwait(false);

        var query = new AuditLogQuery
        {
            UserId = request.UserId,
            TenantId = request.TenantId,
            ActionType = request.ActionType,
            ResourceType = request.ResourceType,
            Category = request.Category,
            RiskLevel = request.RiskLevel,
            Success = request.Success,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IpAddress = request.IpAddress,
            Skip = request.Skip,
            Take = Math.Min(request.Take, 1000) // Cap at 1000 records
        };

        var logs = await auditService.GetAuditLogsAsync(query).ConfigureAwait(false);
        var totalCount = await auditService.GetAuditLogCountAsync(query).ConfigureAwait(false);

        var response = new AuditLogResponse { Logs = logs.Select(MapToDto).ToList(), TotalCount = totalCount, Skip = request.Skip, Take = request.Take };

        return Ok(response);
    }

    /// <summary>
    /// Get audit log statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<ActionResult<AuditStatisticsResponse>> GetAuditStatistics([FromQuery] AuditStatisticsRequest request)
    {
        var adminUserId = GetCurrentUserId();
        if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

        await auditService.LogAdminActionAsync(adminUserId.Value, "ViewAuditStatistics", "Admin accessed audit statistics").ConfigureAwait(false);

        var startDate = request.StartDate ?? SystemClock.UtcNow.AddDays(-30);
        var endDate = request.EndDate ?? SystemClock.UtcNow;

        // Get statistics for different categories
        var authenticationQuery = new AuditLogQuery { Category = AuditCategory.Authentication, StartDate = startDate, EndDate = endDate };

        var permissionQuery = new AuditLogQuery { Category = AuditCategory.Permission, StartDate = startDate, EndDate = endDate };

        var securityQuery = new AuditLogQuery { Category = AuditCategory.Security, StartDate = startDate, EndDate = endDate };

        var failedQuery = new AuditLogQuery { Success = false, StartDate = startDate, EndDate = endDate };

        var highRiskQuery = new AuditLogQuery { RiskLevel = AuditRiskLevel.High, StartDate = startDate, EndDate = endDate };

        var totalEvents = await auditService.GetAuditLogCountAsync(new AuditLogQuery { StartDate = startDate, EndDate = endDate }).ConfigureAwait(false);
        var authenticationEvents = await auditService.GetAuditLogCountAsync(authenticationQuery).ConfigureAwait(false);
        var permissionEvents = await auditService.GetAuditLogCountAsync(permissionQuery).ConfigureAwait(false);
        var securityEvents = await auditService.GetAuditLogCountAsync(securityQuery).ConfigureAwait(false);
        var failedEvents = await auditService.GetAuditLogCountAsync(failedQuery).ConfigureAwait(false);
        var highRiskEvents = await auditService.GetAuditLogCountAsync(highRiskQuery).ConfigureAwait(false);

        var response = new AuditStatisticsResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEvents = totalEvents,
            AuthenticationEvents = authenticationEvents,
            PermissionEvents = permissionEvents,
            SecurityEvents = securityEvents,
            FailedEvents = failedEvents,
            HighRiskEvents = highRiskEvents
        };

        return Ok(response);
    }

    /// <summary>
    /// Export audit logs (admin only)
    /// </summary>
    [HttpPost(":export")]
    [HttpPost("export/csv")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportAuditLogs([FromBody] AuditExportRequest request)
    {
        var adminUserId = GetCurrentUserId();
        if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

        var requestValidation = ValidateExportRequest(request);
        if (requestValidation is not null) { return requestValidation; }

        if (!AuditCsvExporter.TryResolveColumns(request.Columns, out var columns, out var columnError))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid CSV export columns",
                Detail = columnError,
                Status = StatusCodes.Status400BadRequest
            });
        }

        var exportId = Guid.NewGuid();
        var cancellationToken = HttpContext.RequestAborted;
        var export = await sender.Send(
            new ExportAuditLogsCommand(adminUserId.Value, exportId, request),
            cancellationToken).ConfigureAwait(false);
        await exportProgressTracker.BeginAsync(exportId, adminUserId.Value, export.TotalCount, cancellationToken).ConfigureAwait(false);

        var fileName = $"audit-logs-{SystemClock.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv";
        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = "text/csv; charset=utf-8";
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";
        Response.Headers["X-Audit-Export-Id"] = exportId.ToString("D");
        Response.Headers["X-Audit-Total-Records"] = export.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (export.PageNumber.HasValue)
        {
            Response.Headers["X-Audit-Page"] = export.PageNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Response.Headers["X-Audit-Page-Size"] = export.PageSize!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        try
        {
            await AuditCsvExporter.WriteAsync(
                Response.Body,
                TrackExportProgressAsync(export, adminUserId.Value, cancellationToken),
                columns,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new EmptyResult();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "CSV audit export {ExportId} failed", exportId);
            if (Response.HasStarted)
            {
                HttpContext.Abort();
                return new EmptyResult();
            }

            return Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Audit export failed",
                detail: "The audit export could not be completed. Use the export ID when contacting support.");
        }

        return new EmptyResult();
    }

    /// <summary>
    /// Streams a versioned JSON audit export with pagination metadata.
    /// </summary>
    [HttpPost("export/json")]
    [EnableRateLimiting(RateLimitPolicies.ExpensiveOperations)]
    [Produces("application/json")]
    [ProducesResponseType(typeof(AuditJsonExportDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ExportAuditLogsJson([FromBody] AuditExportRequest request)
    {
        var adminUserId = GetCurrentUserId();
        if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

        var requestValidation = ValidateExportRequest(request);
        if (requestValidation is not null) { return requestValidation; }

        request.PageNumber ??= 1;
        request.PageSize ??= 500;

        var exportId = Guid.NewGuid();
        var cancellationToken = HttpContext.RequestAborted;
        var export = await sender.Send(
            new ExportAuditLogsCommand(adminUserId.Value, exportId, request),
            cancellationToken).ConfigureAwait(false);
        await exportProgressTracker.BeginAsync(exportId, adminUserId.Value, export.TotalCount, cancellationToken).ConfigureAwait(false);

        var pageSize = request.PageSize.Value;
        var totalPages = export.TotalCount == 0 ? 0 : (int)Math.Ceiling(export.TotalCount / (double)pageSize);
        Response.ContentType = "application/json; charset=utf-8";
        Response.Headers["Content-Disposition"] = $"attachment; filename=\"audit-logs-{SystemClock.UtcNow:yyyy-MM-dd-HH-mm-ss}.json\"";
        Response.Headers["X-Audit-Export-Id"] = exportId.ToString("D");
        Response.Headers["X-Audit-Total-Records"] = export.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Audit-Page"] = request.PageNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        Response.Headers["X-Audit-Page-Size"] = pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture);

        var document = new AuditJsonExportDocument(
            "1.0",
            new AuditJsonExportPagination(request.PageNumber.Value, pageSize, export.TotalCount, totalPages),
            MapJsonRecordsAsync(TrackExportProgressAsync(export, adminUserId.Value, cancellationToken), cancellationToken));

        return Ok(document);
    }

    /// <summary>Returns the current state of an export started by the authenticated administrator.</summary>
    [HttpGet("export/{exportId:guid}/progress")]
    [ProducesResponseType(typeof(AuditExportProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuditExportProgressResponse>> GetAuditExportProgress(Guid exportId)
    {
        var adminUserId = GetCurrentUserId();
        if (!adminUserId.HasValue) throw new UnauthorizedAccessException("User not authenticated");

        var progress = await exportProgressTracker.GetAsync(exportId, adminUserId.Value, HttpContext.RequestAborted).ConfigureAwait(false);
        return progress is null ? NotFound() : Ok(progress);
    }

    private ActionResult? ValidateExportRequest(AuditExportRequest request)
    {
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (Validator.TryValidateObject(request, new ValidationContext(request), validationResults, validateAllProperties: true))
        {
            return null;
        }

        var errors = validationResults
            .SelectMany(result => result.MemberNames.DefaultIfEmpty("request"), (result, member) => new { member, result.ErrorMessage })
            .GroupBy(error => error.member, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage ?? "Invalid value.").ToArray(), StringComparer.Ordinal);

        return BadRequest(new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest
        });
    }

    private async IAsyncEnumerable<AuditJsonExportRecord> MapJsonRecordsAsync(
        IAsyncEnumerable<AuditLog> auditLogs,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var auditLog in auditLogs.WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            yield return AuditJsonExportMapper.Map(auditLog);
        }
    }

    private async IAsyncEnumerable<AuditLog> TrackExportProgressAsync(
        AuditLogExportData export,
        Guid ownerUserId,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var recordsWritten = 0;
        var completed = false;
        var lastProgressReportTimestamp = Stopwatch.GetTimestamp();
        await using var enumerator = export.Records.GetAsyncEnumerator(cancellationToken);

        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Reading audit export {ExportId} failed", export.ExportId);
                    throw;
                }

                if (!hasNext) { break; }

                recordsWritten++;
                if (recordsWritten % 100 == 0 || Stopwatch.GetElapsedTime(lastProgressReportTimestamp) >= TimeSpan.FromSeconds(2))
                {
                    await exportProgressTracker.ReportAsync(
                        export.ExportId,
                        ownerUserId,
                        recordsWritten,
                        AuditExportProgressStatus.InProgress,
                        null,
                        cancellationToken).ConfigureAwait(false);
                    lastProgressReportTimestamp = Stopwatch.GetTimestamp();
                }

                yield return enumerator.Current;
            }

            await exportProgressTracker.ReportAsync(
                export.ExportId,
                ownerUserId,
                recordsWritten,
                AuditExportProgressStatus.Completed,
                null,
                CancellationToken.None).ConfigureAwait(false);
            completed = true;
        }
        finally
        {
            if (!completed)
            {
                var status = cancellationToken.IsCancellationRequested
                    ? AuditExportProgressStatus.Cancelled
                    : AuditExportProgressStatus.Failed;
                var message = status == AuditExportProgressStatus.Failed
                    ? "The export stopped before all records were written."
                    : null;

                try
                {
                    await exportProgressTracker.ReportAsync(
                        export.ExportId,
                        ownerUserId,
                        recordsWritten,
                        status,
                        message,
                        CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception progressException)
                {
                    _logger.LogError(progressException, "Updating progress for audit export {ExportId} failed", export.ExportId);
                }
            }
        }
    }

    private AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            ActionType = log.ActionType,
            ResourceType = log.ResourceType,
            ResourceId = log.ResourceId,
            UserId = log.UserId,
            TenantId = log.TenantId,
            IpAddress = log.IpAddress,
            UserAgent = log.UserAgent,
            SessionId = log.SessionId,
            Description = log.Description,
            Success = log.Success,
            ErrorMessage = log.ErrorMessage,
            RiskLevel = log.RiskLevel,
            Category = log.Category,
            CorrelationId = log.CorrelationId,
            CreatedAt = log.CreatedAt
        };
    }

    private string GenerateCsv(List<AuditLog> logs)
    {
        var csv = new System.Text.StringBuilder();

        // Header
        csv.AppendLine("Id,ActionType,ResourceType,ResourceId,UserId,TenantId,IpAddress,Description,Success,RiskLevel,Category,CreatedAt");

        // Data rows
        foreach (var log in logs)
        {
            csv.AppendLine(
                $"{log.Id},{log.ActionType},{log.ResourceType},{log.ResourceId},{log.UserId},{log.TenantId},{log.IpAddress},\"{log.Description}\",{log.Success},{log.RiskLevel},{log.Category},{log.CreatedAt:yyyy-MM-dd HH:mm:ss}"
            );
        }

        return csv.ToString();
    }
}

// Request/Response DTOs
