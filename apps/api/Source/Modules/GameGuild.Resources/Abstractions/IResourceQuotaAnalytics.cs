namespace GameGuild.Resources;

/// <summary>
///     Analytics and reporting operations for resource quotas.
///     Use this interface for dashboards and reporting features.
/// </summary>
/// <remarks>
///     Part of the ISP-compliant split of IResourceQuotaService.
///     Admin dashboards and analytics services should depend on this interface.
/// </remarks>
public interface IResourceQuotaAnalytics
{
    /// <summary>
    ///     Get detailed usage information for a specific resource type
    /// </summary>
    Task<ResourceUsageResponse> GetResourceUsageDetailsAsync(Guid tenantId, ResourceUsageType type, int historyDays = 30, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get tenants that have exceeded their limits
    /// </summary>
    Task<IEnumerable<Guid>> GetTenantsExceedingLimitsAsync(ResourceUsageType? type = null, bool hardLimitOnly = false, CancellationToken cancellationToken = default);
}
