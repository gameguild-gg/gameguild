using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to check if the quota allows the given resource consumption for a user
/// </summary>
/// <param name="UserId">The user ID to check quota for</param>
/// <param name="Type">The type of resource usage to check</param>
/// <param name="Amount">The amount of resource to consume (default is 1)</param>
public sealed record CheckUserResourceQuotaQuery(Guid UserId, ResourceUsageType Type, long Amount = 1) : IQuery<ResourceQuotaEnforcementResult>;
