
namespace GameGuild.Resources;

/// <summary>
///     Exception thrown when a resource quota is exceeded
/// </summary>
public class QuotaExceededException : Exception
{
    /// <summary>
    ///     Creates a new QuotaExceededException
    /// </summary>
    /// <param name="resourceType">The type of resource that exceeded quota</param>
    /// <param name="currentUsage">Current usage amount</param>
    /// <param name="limit">The quota limit that was exceeded</param>
    /// <param name="tenantId">The tenant ID that exceeded quota</param>
    public QuotaExceededException(
        ResourceUsageType resourceType,
        long currentUsage,
        long limit,
        Guid tenantId
    ) : base($"Resource quota exceeded for {resourceType}. Current usage: {currentUsage}, Limit: {limit}")
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        TenantId = tenantId;
    }

    /// <summary>
    ///     Creates a new QuotaExceededException with custom message
    /// </summary>
    /// <param name="message">Custom error message</param>
    /// <param name="resourceType">The type of resource that exceeded quota</param>
    /// <param name="currentUsage">Current usage amount</param>
    /// <param name="limit">The quota limit that was exceeded</param>
    /// <param name="tenantId">The tenant ID that exceeded quota</param>
    public QuotaExceededException(
        string message,
        ResourceUsageType resourceType,
        long currentUsage,
        long limit,
        Guid tenantId
    ) : base(message)
    {
        ResourceType = resourceType;
        CurrentUsage = currentUsage;
        Limit = limit;
        TenantId = tenantId;
    }

    /// <summary>
    ///     The type of resource that exceeded quota
    /// </summary>
    public ResourceUsageType ResourceType { get; }

    /// <summary>
    ///     Current usage amount
    /// </summary>
    public long CurrentUsage { get; }

    /// <summary>
    ///     The quota limit that was exceeded
    /// </summary>
    public long Limit { get; }

    /// <summary>
    ///     The tenant ID that exceeded quota
    /// </summary>
    public Guid TenantId { get; }

    /// <summary>
    ///     Gets remaining quota (will be 0 or negative when exceeded)
    /// </summary>
    public long RemainingQuota => Math.Max(0, Limit - CurrentUsage);
}
