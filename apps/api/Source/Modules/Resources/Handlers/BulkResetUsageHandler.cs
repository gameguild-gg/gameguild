using GameGuild.Core.Messaging;
using GameGuild.Modules.Resources.Commands;
using GameGuild.Modules.Resources.Contexts;
using GameGuild.Modules.Resources.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Modules.Resources.Handlers;

/// <summary>
/// Handler for BulkResetUsageCommand
/// </summary>
public class BulkResetUsageHandler(
    ApplicationDbContext context,
    ILogger<BulkResetUsageHandler> logger)
    : IRequestHandler<BulkResetUsageCommand, Result<BulkResetUsageResult>>
{
    public async Task<Result<BulkResetUsageResult>> Handle(
        BulkResetUsageCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation(
                "Starting bulk reset for {TenantCount} tenants, type filter: {UsageType}",
                request.TenantIds.Count, request.UsageType?.ToString() ?? "ALL");

            var result = new BulkResetUsageResult();

            foreach (var tenantId in request.TenantIds)
            {
                try
                {
                    var recordsReset = await ResetTenantUsageAsync(tenantId, request.UsageType, cancellationToken);
                    result.SuccessCount++;
                    result.TotalRecordsReset += recordsReset;

                    logger.LogDebug("Reset {Count} records for tenant {TenantId}", recordsReset, tenantId);
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Failures.Add(new BulkResetFailure
                    {
                        TenantId = tenantId,
                        ErrorMessage = ex.Message
                    });

                    logger.LogWarning(ex, "Failed to reset usage for tenant {TenantId}", tenantId);
                }
            }

            logger.LogInformation(
                "Bulk reset complete: {Success} successful, {Failure} failed, {Total} total records reset",
                result.SuccessCount, result.FailureCount, result.TotalRecordsReset);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during bulk reset operation");
            return Result.Failure<BulkResetUsageResult>($"Bulk reset operation failed: {ex.Message}");
        }
    }

    private async Task<int> ResetTenantUsageAsync(
        Guid tenantId,
        ResourceUsageType? usageType,
        CancellationToken cancellationToken)
    {
        // Build query for quotas to reset
        var quotaQuery = context.Set<ResourceQuota>()
            .Where(q => q.TenantId == tenantId);

        if (usageType.HasValue)
        {
            quotaQuery = quotaQuery.Where(q => q.Type == usageType.Value);
        }

        var quotas = await quotaQuery.ToListAsync(cancellationToken);

        if (quotas.Count == 0)
        {
            return 0;
        }

        // Reset current usage to 0 and update LastReset
        foreach (var quota in quotas)
        {
            quota.CurrentUsage = 0;
            quota.LastReset = DateTime.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);

        return quotas.Count;
    }
}
