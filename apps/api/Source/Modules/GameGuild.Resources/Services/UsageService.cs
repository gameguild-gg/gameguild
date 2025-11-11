using GameGuild.Resources.Abstractions;
using GameGuild.Resources.Models;
using Microsoft.Extensions.Logging;

namespace GameGuild.Resources.Services;

/// <summary>
///     Basic implementation of usage tracking service
/// </summary>
public class UsageService(ILogger<UsageService> logger) : IUsageService
{
    public async Task<TenantResourceUsage> GetCurrentUsageAsync(Guid tenantId)
    {
        // This would need to be implemented based on the actual TenantResourceUsage model
        await Task.Delay(1);

        throw new NotImplementedException("GetCurrentUsageAsync requires TenantResourceUsage model implementation");
    }

    public async Task<IEnumerable<ResourceUsageHistory>> GetUsageHistoryAsync(Guid tenantId, int months)
    {
        // This would need to be implemented based on the actual ResourceUsageHistory model
        await Task.Delay(1);
        logger.LogWarning("GetUsageHistoryAsync not fully implemented");

        throw new NotImplementedException("GetUsageHistoryAsync requires ResourceUsageHistory model implementation");
    }

    public async Task TrackUsageAsync(Guid tenantId, string resourceType, int amount)
    {
        // This would need to be implemented to track usage
        logger.LogInformation("Tracking usage for tenant {TenantId}, type {ResourceType}, amount {Amount}", tenantId, resourceType, amount);
        await Task.CompletedTask;

        throw new NotImplementedException("TrackUsageAsync not fully implemented");
    }

    public async Task<bool> IsWithinLimitsAsync(Guid tenantId, string resourceType, int requestedAmount)
    {
        // This would need to be implemented to check limits
        logger.LogInformation("Checking limits for tenant {TenantId}, type {ResourceType}, amount {RequestedAmount}", tenantId, resourceType, requestedAmount);
        await Task.CompletedTask;

        return true; // Default to allowing for now
    }
}
