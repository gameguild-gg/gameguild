using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;

namespace GameGuild.Compliance.Audit;

public sealed record ExportAuditLogsCommand(
    Guid AdminUserId,
    Guid ExportId,
    AuditExportRequest Request) : ICommand<AuditLogExportData>;

public sealed record AuditLogExportData(
    Guid ExportId,
    int TotalCount,
    int? PageNumber,
    int? PageSize,
    IAsyncEnumerable<AuditLog> Records);

public sealed record ExportSecurityAuditLogsCommand(
    Guid AdminUserId,
    UnifiedSecurityAuditRequest Request) : ICommand<byte[]>;

public sealed class AuditExportCommandHandler(
    IAuditService auditService,
    ISecurityAuditAggregator auditAggregator,
    IBulkExportMonitoringService bulkExports,
    IActorContextAccessor actors) :
    ICommandHandler<ExportAuditLogsCommand, AuditLogExportData>,
    ICommandHandler<ExportSecurityAuditLogsCommand, byte[]>
{
    public async Task<AuditLogExportData> Handle(
        ExportAuditLogsCommand command,
        CancellationToken cancellationToken)
    {
        await auditService.LogAdminActionAsync(
            command.AdminUserId,
            "ExportAuditLogs",
            "Admin exported audit logs",
            new
            {
                ExportId = command.ExportId,
                RequestedBy = command.AdminUserId,
                HasCompletionWebhook = !string.IsNullOrWhiteSpace(command.Request.WebhookUrl)
            }).ConfigureAwait(false);

        var query = new AuditLogQuery
        {
            UserId = command.Request.UserId,
            TenantId = command.Request.TenantId,
            ActionType = command.Request.ActionType,
            ResourceType = command.Request.ResourceType,
            Category = command.Request.Category,
            RiskLevel = command.Request.RiskLevel,
            Success = command.Request.Success,
            StartDate = command.Request.StartDate,
            EndDate = command.Request.EndDate,
            IpAddress = command.Request.IpAddress,
            Condition = command.Request.Condition,
            Skip = command.Request.PageNumber.HasValue && command.Request.PageSize.HasValue
                ? (command.Request.PageNumber.Value - 1) * command.Request.PageSize.Value
                : 0,
            Take = command.Request.PageSize ?? 0
        };

        var totalCount = await auditService.GetAuditLogCountAsync(query).ConfigureAwait(false);
        await RecordBulkExportEvaluationAsync(command, totalCount, cancellationToken).ConfigureAwait(false);
        var records = auditService.StreamAuditLogsAsync(query, cancellationToken);

        return new AuditLogExportData(
            command.ExportId,
            totalCount,
            command.Request.PageNumber,
            command.Request.PageSize,
            records);
    }

    /// <summary>
    /// Bulk data export detection (issues #180/#186): every audit-log export flows through the
    /// bulk-export monitor with the <see cref="BulkExportScope.AuditLogExport"/> scope, so exports
    /// exceeding the configured record thresholds raise a durable security alert and, when blocking
    /// is enabled, are refused. A platform-wide export by a tenantless system administrator has no
    /// tenant ledger to record against; its admin action is already journaled above.
    /// </summary>
    private async Task RecordBulkExportEvaluationAsync(
        ExportAuditLogsCommand command,
        int totalCount,
        CancellationToken cancellationToken)
    {
        var actor = actors.ActorContext;
        var tenantId = command.Request.TenantId ?? actor.TenantId;
        if (tenantId is null)
        {
            return;
        }

        var evaluation = await bulkExports.EvaluateAndRecordAsync(
            tenantId.Value,
            command.AdminUserId,
            BulkExportScope.AuditLogExport,
            totalCount,
            actor.TypedAttributes.IpAddress,
            command.ExportId.ToString("D"),
            destination: null,
            cancellationToken).ConfigureAwait(false);

        if (evaluation.Blocked)
        {
            throw new FieldLevelAuditExportBlockedException(evaluation.Event.RecordCount, evaluation.Event.Threshold);
        }
    }

    public async Task<byte[]> Handle(
        ExportSecurityAuditLogsCommand command,
        CancellationToken cancellationToken)
    {
        await auditService.LogAdminActionAsync(
            command.AdminUserId,
            "ExportSecurityAuditLogs",
            "Admin exported unified security audit logs",
            new { Filters = command.Request }).ConfigureAwait(false);
        return await auditAggregator.ExportAuditLogsAsync(command.Request, cancellationToken).ConfigureAwait(false);
    }
}
