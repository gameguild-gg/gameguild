using GameGuild.Database;
using GameGuild.Modules.Resources.Abstractions;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Resources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GameGuild.Modules.Resources.Services;

/// <summary>
/// Implementation of cost allocation and chargeback reporting
/// </summary>
public class CostAllocationService : ICostAllocationService {
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CostAllocationService> _logger;

    // Default cost per unit by resource type (can be configured)
    private readonly Dictionary<ResourceUsageType, decimal> _costPerUnit = new()
    {
        { ResourceUsageType.Compute, 0.10m },
        { ResourceUsageType.Storage, 0.05m },
        { ResourceUsageType.Bandwidth, 0.02m },
        { ResourceUsageType.ApiCalls, 0.001m },
        { ResourceUsageType.Database, 0.15m },
        { ResourceUsageType.Users, 5.00m },
        { ResourceUsageType.Custom, 0.01m }
    };

    public CostAllocationService(
        ApplicationDbContext context,
        ILogger<CostAllocationService> logger) {
        _context = context;
        _logger = logger;
    }

    public async Task<CostAllocationReport> GenerateReportAsync(
        Guid tenantId,
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Generating cost allocation report for tenant {TenantId} from {Start} to {End}",
            tenantId, periodStart, periodEnd);

        var usageRecords = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.TenantId == tenantId &&
                        r.RecordedAt >= periodStart &&
                        r.RecordedAt < periodEnd)
            .ToListAsync(cancellationToken);

        var groupedByType = usageRecords
            .GroupBy(r => r.UsageType)
            .Select(g => new {
                UsageType = g.Key,
                TotalUsage = g.Sum(r => r.Count)
            })
            .ToList();

        var reports = new List<CostAllocationReport>();

        foreach (var group in groupedByType) {
            var costPerUnit = await CalculateCostAsync(group.UsageType, 1, cancellationToken);
            var totalCost = costPerUnit * group.TotalUsage;

            // Get allocation tags from resource quotas
            var tags = await GetAllocationTagsAsync(tenantId, group.UsageType, cancellationToken);

            var report = new CostAllocationReport {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                UsageType = group.UsageType,
                TotalUsage = group.TotalUsage,
                CostPerUnit = costPerUnit,
                TotalCost = totalCost,
                AllocationTags = JsonSerializer.Serialize(tags),
                CostCenter = tags.GetValueOrDefault("CostCenter"),
                Project = tags.GetValueOrDefault("Project"),
                Owner = tags.GetValueOrDefault("Owner"),
                IsExported = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Set<CostAllocationReport>().Add(report);
            reports.Add(report);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Generated {Count} cost allocation reports for tenant {TenantId}",
            reports.Count, tenantId);

        // Return consolidated report
        return reports.FirstOrDefault() ?? new CostAllocationReport {
            TenantId = tenantId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TotalCost = 0
        };
    }

    public async Task<List<CostAllocationReport>> GenerateAllReportsAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Generating cost allocation reports for all tenants from {Start} to {End}",
            periodStart, periodEnd);

        var tenantIds = await _context.Set<ResourceUsageRecord>()
            .Where(r => r.RecordedAt >= periodStart && r.RecordedAt < periodEnd)
            .Select(r => r.TenantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var allReports = new List<CostAllocationReport>();

        foreach (var tenantId in tenantIds) {
            try {
                var report = await GenerateReportAsync(tenantId, periodStart, periodEnd, cancellationToken);
                allReports.Add(report);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Failed to generate report for tenant {TenantId}", tenantId);
            }
        }

        return allReports;
    }

    public Task<decimal> CalculateCostAsync(
        ResourceUsageType usageType,
        long usage,
        CancellationToken cancellationToken = default) {
        var costPerUnit = _costPerUnit.GetValueOrDefault(usageType, 0.01m);
        return Task.FromResult(costPerUnit * usage);
    }

    public async Task<List<CostAllocationReport>> GetReportsByTenantAsync(
        Guid tenantId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default) {
        var query = _context.Set<CostAllocationReport>()
            .Where(r => r.TenantId == tenantId);

        if (startDate.HasValue)
            query = query.Where(r => r.PeriodStart >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(r => r.PeriodEnd <= endDate.Value);

        return await query
            .OrderByDescending(r => r.PeriodStart)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ExportReportsAsync(
        List<Guid> reportIds,
        string invoiceReference,
        CancellationToken cancellationToken = default) {
        _logger.LogInformation("Exporting {Count} cost allocation reports with invoice {Invoice}",
            reportIds.Count, invoiceReference);

        var reports = await _context.Set<CostAllocationReport>()
            .Where(r => reportIds.Contains(r.Id) && !r.IsExported)
            .ToListAsync(cancellationToken);

        foreach (var report in reports) {
            report.IsExported = true;
            report.ExportedAt = DateTime.UtcNow;
            report.InvoiceReference = invoiceReference;
            report.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Exported {Count} reports", reports.Count);
        return reports.Count;
    }

    public async Task<Dictionary<string, decimal>> GetChargebackByCostCenterAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default) {
        return await _context.Set<CostAllocationReport>()
            .Where(r => r.PeriodStart >= periodStart &&
                        r.PeriodEnd <= periodEnd &&
                        r.CostCenter != null)
            .GroupBy(r => r.CostCenter!)
            .Select(g => new { CostCenter = g.Key, TotalCost = g.Sum(r => r.TotalCost) })
            .ToDictionaryAsync(x => x.CostCenter, x => x.TotalCost, cancellationToken);
    }

    public async Task<Dictionary<string, decimal>> GetChargebackByProjectAsync(
        DateTime periodStart,
        DateTime periodEnd,
        CancellationToken cancellationToken = default) {
        return await _context.Set<CostAllocationReport>()
            .Where(r => r.PeriodStart >= periodStart &&
                        r.PeriodEnd <= periodEnd &&
                        r.Project != null)
            .GroupBy(r => r.Project!)
            .Select(g => new { Project = g.Key, TotalCost = g.Sum(r => r.TotalCost) })
            .ToDictionaryAsync(x => x.Project, x => x.TotalCost, cancellationToken);
    }

    private async Task<Dictionary<string, string>> GetAllocationTagsAsync(
        Guid tenantId,
        ResourceUsageType usageType,
        CancellationToken cancellationToken) {
        var quota = await _context.Set<ResourceQuota>()
            .FirstOrDefaultAsync(q => q.TenantId == tenantId && q.UsageType == usageType, cancellationToken);

        if (quota == null)
            return new Dictionary<string, string>();

        var tags = await _context.Set<ResourceTag>()
            .Where(t => t.ResourceQuotaId == quota.Id && t.IncludeInCostAllocation)
            .ToListAsync(cancellationToken);

        return tags.ToDictionary(t => t.Key, t => t.Value);
    }
}
