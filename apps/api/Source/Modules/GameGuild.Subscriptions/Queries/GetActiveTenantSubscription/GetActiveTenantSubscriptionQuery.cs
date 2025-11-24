using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query to get active subscription for tenant
/// </summary>
public record GetActiveTenantSubscriptionQuery(Guid TenantId) : IQuery<Subscription?>;
