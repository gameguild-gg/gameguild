using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get resource usage records for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="ResourceUsageType">Optional usage type filter</param>
/// <param name="StartDate">Optional start date filter</param>
/// <param name="EndDate">Optional end date filter</param>
public sealed record GetUserResourceUsageRecordsQuery(Guid UserId, ResourceUsageType? ResourceUsageType = null, DateTime? StartDate = null, DateTime? EndDate = null) : IQuery<IEnumerable<UsageRecord>>;
