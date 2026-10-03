namespace GameGuild.Compliance.Audit;

public interface IAuditActionTypeSearchService
{
    Task<AuditActionTypeSearchResult> SearchAsync(
        AuditActionTypeSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<AuditActionTypeExportResult> ExportAsync(
        AuditActionTypeSearchRequest request,
        int maximumRecords,
        CancellationToken cancellationToken = default);
}
