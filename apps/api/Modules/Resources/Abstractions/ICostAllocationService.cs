using GameGuild.Modules.Resources.Entities;

namespace GameGuild.Modules.Resources.Abstractions;

/// <summary>
/// Service for cost allocation and chargeback reporting
/// </summary>
public interface ICostAllocationService
{
    /// <summary>
    /// Generate cost allocation report for a tenant and period
    /// </summary>
    Task<CostAllocationReport> GenerateReportAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate cost allocation reports for all tenants
    /// </summary>
    Task<List<CostAllocationReport>> GenerateAllReportsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate cost for specific usage
    /// </summary>
    Task<decimal> CalculateCostAsync(
        ResourceUsageType usageType,
        long usage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cost allocation reports by tenant
    /// </summary>
    Task<List<CostAllocationReport>> GetReportsByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export cost allocation reports to billing system
    /// </summary>
    Task<int> ExportReportsAsync(
        List<Guid> reportIds,
        string invoiceReference,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chargeback summary by cost center
    /// </summary>
    Task<Dictionary<string, decimal>> GetChargebackByCostCenterAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get chargeback summary by project
    /// </summary>
    Task<Dictionary<string, decimal>> GetChargebackByProjectAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default);
}
