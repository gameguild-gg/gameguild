
namespace GameGuild.Resources;

/// <summary>
///     Service for managing resource throttling policies
/// </summary>
public interface IResourceThrottlingService
{
    /// <summary>
    ///     Create or update a throttling policy
    /// </summary>
    Task<ResourceThrottlingPolicy> SetPolicyAsync(Guid tenantId, ResourceUsageType type, ThrottlingStrategy strategy, long threshold, string? configuration = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get throttling policy for a tenant and resource type
    /// </summary>
    Task<ResourceThrottlingPolicy?> GetPolicyAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get all policies for a tenant
    /// </summary>
    Task<IEnumerable<ResourceThrottlingPolicy>> GetTenantPoliciesAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a throttling policy
    /// </summary>
    Task<bool> DeletePolicyAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Check if a resource request should be throttled
    /// </summary>
    Task<(bool ShouldBlock, int DelayMs)> ShouldThrottleAsync(Guid tenantId, ResourceUsageType type, long currentUsage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Apply throttling policy to a resource request
    /// </summary>
    Task<ThrottlingResult> ApplyThrottlingAsync(Guid tenantId, ResourceUsageType type, long requestedAmount = 1, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Get active throttling policies across all tenants
    /// </summary>
    Task<IEnumerable<ResourceThrottlingPolicy>> GetActivePoliciesAsync(ResourceUsageType? type = null, CancellationToken cancellationToken = default);
}
