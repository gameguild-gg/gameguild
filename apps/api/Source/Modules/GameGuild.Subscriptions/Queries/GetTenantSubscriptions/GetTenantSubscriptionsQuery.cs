using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query to get all subscriptions for a tenant
/// </summary>
public record GetTenantSubscriptionsQuery(Guid TenantId) : IQuery<IEnumerable<Subscription>>;
