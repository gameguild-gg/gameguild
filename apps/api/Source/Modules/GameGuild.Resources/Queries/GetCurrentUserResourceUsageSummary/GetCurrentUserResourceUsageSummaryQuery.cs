using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get current resource usage summary for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
public sealed record GetCurrentUserResourceUsageSummaryQuery(Guid UserId) : IQuery<Dictionary<ResourceUsageType, long>>;
