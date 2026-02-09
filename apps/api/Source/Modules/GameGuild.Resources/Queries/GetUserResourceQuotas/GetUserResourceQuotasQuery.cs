using GameGuild.CQRS;

namespace GameGuild.Resources;

/// <summary>
///     Query to get all quotas for a specific user
/// </summary>
/// <param name="UserId">The user ID to get quotas for</param>
public sealed record GetUserResourceQuotasQuery(Guid UserId) : IQuery<IEnumerable<ResourceQuotaResponse>>;
