namespace GameGuild.Resources;

/// <summary>
///     Write operations for resource quota configuration.
///     Use this interface for admin operations that modify quota definitions.
/// </summary>
/// <remarks>
///     Part of the ISP-compliant split of IResourceQuotaService.
///     Only admin/subscription handlers should depend on this interface.
/// </remarks>
public interface IResourceQuotaWriter
{
    /// <summary>
    ///     Create or update a resource quota for a tenant
    /// </summary>
    Task<ResourceQuota> SetQuotaAsync(Guid tenantId, ResourceUsageType type, long? softLimit, long? hardLimit, ResourceQuotaPeriod period = ResourceQuotaPeriod.Monthly, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Delete a resource quota
    /// </summary>
    Task<bool> DeleteQuotaAsync(Guid tenantId, ResourceUsageType type, CancellationToken cancellationToken = default);
}
