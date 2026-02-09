using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get all subscriptions for a tenant
/// </summary>
public sealed record GetTenantSubscriptionsQuery(Guid TenantId) : IQuery<IEnumerable<Subscription>>;
