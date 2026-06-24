
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

    // PLANNED: Integration with Billing module for invoice generation (depends on GameGuild.Commerce.Billing)
    // PLANNED: Integration with Finance module for cost center validation (depends on GameGuild.Finance)
}
