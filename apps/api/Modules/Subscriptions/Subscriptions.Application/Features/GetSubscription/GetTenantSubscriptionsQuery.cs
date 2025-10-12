using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query to get all subscriptions for a tenant
/// </summary>
public record GetTenantSubscriptionsQuery(Guid TenantId) : IQuery<IEnumerable<Subscription>>;

