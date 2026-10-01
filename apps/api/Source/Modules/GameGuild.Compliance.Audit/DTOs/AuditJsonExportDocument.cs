using System.Text.Json;

namespace GameGuild.Compliance.Audit;

public sealed record AuditJsonExportDocument(
    string SchemaVersion,
    AuditJsonExportPagination Pagination,
    IAsyncEnumerable<AuditJsonExportRecord> Records);

public sealed record AuditJsonExportPagination(
    int PageNumber,
    int PageSize,
    int TotalRecords,
    int TotalPages);

public sealed record AuditJsonExportRecord(
    Guid Id,
    AuditEventContext Event,
    AuditActorContext Actor,
    AuditResourceContext Resource,
    AuditNetworkContext Network,
    AuditOutcomeContext Outcome,
    string? Description,
    JsonElement? Metadata,
    string? CorrelationId,
    DateTime CreatedAt);

public sealed record AuditEventContext(string ActionType, string Category, string RiskLevel);

public sealed record AuditActorContext(Guid? UserId, Guid? TenantId, Guid? SessionId);

public sealed record AuditResourceContext(string ResourceType, string? ResourceId);

public sealed record AuditNetworkContext(string? IpAddress, string? UserAgent);

public sealed record AuditOutcomeContext(bool Success, string? ErrorMessage);

public static class AuditJsonExportMapper
{
    public static AuditJsonExportRecord Map(AuditLog auditLog)
    {
        return new AuditJsonExportRecord(
            auditLog.Id,
            new AuditEventContext(auditLog.ActionType, auditLog.Category.ToString(), auditLog.RiskLevel.ToString()),
            new AuditActorContext(auditLog.UserId, auditLog.TenantId, auditLog.SessionId),
            new AuditResourceContext(auditLog.ResourceType, auditLog.ResourceId),
            new AuditNetworkContext(auditLog.IpAddress, auditLog.UserAgent),
            new AuditOutcomeContext(auditLog.Success, auditLog.ErrorMessage),
            auditLog.Description,
            ParseMetadata(auditLog.Metadata),
            auditLog.CorrelationId,
            auditLog.CreatedAt);
    }

    private static JsonElement? ParseMetadata(string? metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata)) { return null; }

        try
        {
            using var document = JsonDocument.Parse(metadata);
            var root = document.RootElement;
            return root.ValueKind == JsonValueKind.Object
                ? root.Clone()
                : JsonSerializer.SerializeToElement(new { value = root.Clone() });
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(new { raw = metadata });
        }
    }
}
