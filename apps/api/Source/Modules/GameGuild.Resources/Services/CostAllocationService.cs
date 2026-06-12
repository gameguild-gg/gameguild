using System.Text.Json;
using GameGuild.Billing;
using GameGuild.CQRS;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameGuild.Resources;

/// <summary>
///     Implementation of cost allocation and chargeback reporting
/// </summary>
public class CostAllocationService(
    ICostAllocationReportRepository reportRepository,
    IUsageRecordRepository usageRepository,
    IResourceQuotaRepository quotaRepository,
    IOptions<ResourcesOptions> options,
    ILogger<CostAllocationService> logger,
    ISender? sender = null,
    ICostCenterValidator? costCenterValidator = null
) : ICostAllocationService
{
    private readonly ResourcesOptions _options = options.Value;

    public async Task<CostAllocationReport> GenerateReportAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Generating cost allocation report for tenant {TenantId} from {Start} to {End}", tenantId, periodStart, periodEnd);

        var usageRecords = await usageRepository.GetByTenantAsync(tenantId, null, periodStart, periodEnd, cancellationToken).ConfigureAwait(false);

        var groupedByType = usageRecords.GroupBy(r => r.Type).Select(g => new { UsageType = g.Key, TotalUsage = g.Sum(r => r.UsageAmount) }).ToList();

        decimal totalCost = 0;
        var allocationTags = new Dictionary<string, string>();

        foreach (var group in groupedByType)
        {
            var costPerUnit = GetCostPerUnit(group.UsageType);
            totalCost += costPerUnit * group.TotalUsage;

            // Get allocation tags from resource quotas
            var quota = await quotaRepository.GetByTenantAndTypeAsync(tenantId, group.UsageType, cancellationToken).ConfigureAwait(false);

            if (quota?.Metadata != null)
            {
                // Extract allocation tags from strongly-typed metadata
                var metadata = quota.Metadata;

                // Add custom properties to allocation tags
                if (metadata.CustomProperties != null)
                {
                    foreach (var prop in metadata.CustomProperties) { allocationTags.TryAdd(prop.Key, prop.Value); }
                }

                // Add source and external reference as allocation metadata if present
                if (!string.IsNullOrEmpty(metadata.Source)) { allocationTags.TryAdd("Source", metadata.Source); }

                if (!string.IsNullOrEmpty(metadata.ExternalReferenceId))
                {
                    allocationTags.TryAdd("ExternalReferenceId", metadata.ExternalReferenceId);
                }
            }
        }

        var report = new CostAllocationReport
        {
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalUsage = groupedByType.Sum(g => g.TotalUsage),
            CostPerUnit = totalCost / Math.Max(1, groupedByType.Sum(g => g.TotalUsage)),
            TotalCost = totalCost,
            AllocationTags = allocationTags.Count > 0 ? JsonSerializer.Serialize(allocationTags) : null,
            CostCenter = allocationTags.GetValueOrDefault("CostCenter"),
            Project = allocationTags.GetValueOrDefault("Project"),
            Owner = allocationTags.GetValueOrDefault("Owner"),
            IsExported = false
        };
        report.SetProperties(new Dictionary<string, object?> { ["TenantId"] = tenantId });
        report.CostCenterValidationStatus = await ValidateCostCenterAsync(tenantId, report.CostCenter, cancellationToken).ConfigureAwait(false);

        var savedReport = await reportRepository.AddAsync(report, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Generated cost allocation report {ReportId} for tenant {TenantId} with total cost {TotalCost:C}", savedReport.Id, tenantId, totalCost);

        return savedReport;
    }

    public async Task<IEnumerable<CostAllocationReport>> GetTenantReportsAsync(Guid tenantId, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        return await reportRepository.GetByTenantAsync(tenantId, fromDate, toDate, cancellationToken).ConfigureAwait(false);
    }

    public async Task<CostAllocationReport?> GetReportAsync(Guid reportId, CancellationToken cancellationToken = default) { return await reportRepository.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false); }

    public async Task<decimal> CalculateTotalCostAsync(Guid tenantId, DateTime periodStart, DateTime periodEnd, CancellationToken cancellationToken = default)
    {
        // Get all usage records for tenant and filter by date
        var allRecords = await usageRepository.GetByTenantAsync(tenantId, cancellationToken).ConfigureAwait(false);
        var usageRecords = allRecords.Where(r => r.PeriodStart >= periodStart && r.PeriodStart <= periodEnd);

        decimal totalCost = 0;

        foreach (var group in usageRecords.GroupBy(r => r.Type))
        {
            var costPerUnit = GetCostPerUnit(group.Key);
            var totalUsage = group.Sum(r => r.UsageAmount);
            totalCost += costPerUnit * totalUsage;
        }

        return totalCost;
    }

    public async Task<bool> MarkAsExportedAsync(Guid reportId, string? invoiceReference = null, CancellationToken cancellationToken = default)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);

        if (report == null) return false;

        report.IsExported = true;
        report.ExportedAt = SystemClock.UtcNow;
        report.InvoiceReference = invoiceReference;
        report.Touch();

        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Marked report {ReportId} as exported with invoice reference {InvoiceReference}", reportId, invoiceReference);

        return true;
    }

    public async Task<IEnumerable<CostAllocationReport>> GetUnexportedReportsAsync(CancellationToken cancellationToken = default) { return await reportRepository.GetUnexportedReportsAsync(cancellationToken).ConfigureAwait(false); }

    public async Task<bool> UpdateAllocationTagsAsync(Guid reportId, Dictionary<string, string> tags, CancellationToken cancellationToken = default)
    {
        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);

        if (report == null) return false;

        report.AllocationTags = JsonSerializer.Serialize(tags);
        report.CostCenter = tags.GetValueOrDefault("CostCenter");
        report.CostCenterValidationStatus = await ValidateCostCenterAsync(report.TenantId ?? Guid.Empty, report.CostCenter, cancellationToken).ConfigureAwait(false);
        report.Project = tags.GetValueOrDefault("Project");
        report.Owner = tags.GetValueOrDefault("Owner");
        report.Touch();

        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated allocation tags for report {ReportId}", reportId);

        return true;
    }

    public async Task<CostAllocationInvoiceExportResult?> ExportReportToBillingInvoiceAsync(
        Guid reportId,
        Guid subscriptionId,
        string currency = "USD",
        DateTime? dueDate = null,
        CancellationToken cancellationToken = default)
    {
        if (sender is null)
        {
            throw new InvalidOperationException("Billing invoice export requires the CQRS sender to be registered.");
        }

        var report = await reportRepository.GetByIdAsync(reportId, cancellationToken).ConfigureAwait(false);
        if (report is null)
        {
            return null;
        }

        if (report.TenantId is null)
        {
            throw new InvalidOperationException("Cost allocation report cannot be exported without a tenant id.");
        }

        var invoice = await sender.Send(
            new CreateCostAllocationInvoiceCommand(
                report.TenantId.Value,
                subscriptionId,
                report.TotalCost,
                report.PeriodStart,
                report.PeriodEnd,
                currency,
                dueDate),
            cancellationToken).ConfigureAwait(false);

        report.IsExported = true;
        report.ExportedAt = SystemClock.UtcNow;
        report.InvoiceReference = invoice.InvoiceNumber;
        report.Touch();
        await reportRepository.UpdateAsync(report, cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Exported cost allocation report {ReportId} to invoice {InvoiceNumber}",
            reportId,
            invoice.InvoiceNumber);

        return new CostAllocationInvoiceExportResult(
            report.Id,
            invoice.InvoiceId,
            invoice.InvoiceNumber,
            invoice.Total,
            invoice.DueDate);
    }

    private decimal GetCostPerUnit(ResourceUsageType type)
    {
        var typeName = type.ToString();
        return _options.CostPerUnit.GetValueOrDefault(typeName, _options.DefaultCostPerUnit);
    }

    private async Task<string> ValidateCostCenterAsync(Guid tenantId, string? costCenter, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(costCenter))
        {
            return "NotProvided";
        }

        if (costCenterValidator is null)
        {
            return "NotValidated";
        }

        var validation = await costCenterValidator.ValidateAsync(tenantId, costCenter, cancellationToken).ConfigureAwait(false);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.Message ?? $"Cost center '{costCenter}' is invalid.");
        }

        return validation.Status;
    }
}
