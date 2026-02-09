using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to check resource usage limits for a user
/// </summary>
/// <param name="UserId">User unique identifier</param>
/// <param name="ResourceUsageType">Optional filter by specific resource usage type</param>
public sealed record CheckUserResourceUsageLimitsQuery(Guid UserId, ResourceUsageType? ResourceUsageType = null) : IQuery<Dictionary<ResourceUsageType, bool>>;
