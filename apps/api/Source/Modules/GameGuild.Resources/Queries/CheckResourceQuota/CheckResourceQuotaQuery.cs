using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to check if the quota allows the given resource consumption.
///     <para>
///     <b>ADVISORY ONLY:</b> This query is read-only and does NOT consume or reserve quota.
///     Use for UI/UX purposes such as:
///     - Displaying quota status to users
///     - Showing "approaching limit" warnings
///     - Pre-flight checks before bulk operations
///     </para>
///     <para>
///     For actual quota enforcement, use commands decorated with <c>[RequiresQuota]</c>
///     which internally use <c>TryAtomicConsumeAsync</c> for atomic enforcement.
///     </para>
/// </summary>
/// <param name="TenantId">The tenant ID to check quota for</param>
/// <param name="Type">The type of resource usage to check</param>
/// <param name="Amount">The amount of resource to consume (default is 1)</param>
public sealed record CheckResourceQuotaQuery(Guid TenantId, ResourceUsageType Type, long Amount = 1) : IQuery<ResourceQuotaEnforcementResult>;
