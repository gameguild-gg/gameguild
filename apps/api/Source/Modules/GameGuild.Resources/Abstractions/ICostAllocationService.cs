
namespace GameGuild.Resources;

/// <summary>
///     Service for cost allocation reporting and billing integration
/// </summary>
public interface ICostAllocationService
{
    /// <summary>
    ///     Generate cost allocation report for a tenant and period
    /// </summary>
    Task<CostAllocationReport> GenerateReportAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all reports for a tenant
    /// </summary>
    Task<IEnumerable<CostAllocationReport>> GetTenantReportsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get report by ID
    /// </summary>
    Task<CostAllocationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Calculate total cost for a tenant in a period
    /// </summary>
    Task<decimal> CalculateTotalCostAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Mark a report as exported for billing
    /// </summary>
    Task<bool> MarkAsExportedAsync(Guid reportId, string? invoiceReference = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get unexported reports ready for billing
    /// </summary>
    Task<IEnumerable<CostAllocationReport>> GetUnexportedReportsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Update allocation tags for a report
    /// </summary>
    Task<bool> UpdateAllocationTagsAsync(Guid reportId, Dictionary<string, string> tags, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Export a cost allocation report into the Billing module as an issued invoice.
    /// </summary>
    Task<CostAllocationInvoiceExportResult?> ExportReportToBillingInvoiceAsync(
        Guid reportId,
        Guid subscriptionId,
        string currency = "USD",
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default);
}

public sealed record CostAllocationInvoiceExportResult(
    Guid ReportId,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal TotalCost,
    DateTime? DueDate);
