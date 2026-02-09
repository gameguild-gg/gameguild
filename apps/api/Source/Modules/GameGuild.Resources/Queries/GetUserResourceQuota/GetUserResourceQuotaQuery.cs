using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get a specific quota for a user and resource type
/// </summary>
/// <param name="UserId">The user ID to get quota for</param>
/// <param name="Type">The resource type to get quota for</param>
public sealed record GetUserResourceQuotaQuery(Guid UserId, ResourceUsageType Type) : IQuery<ResourceQuotaResponse?>;
