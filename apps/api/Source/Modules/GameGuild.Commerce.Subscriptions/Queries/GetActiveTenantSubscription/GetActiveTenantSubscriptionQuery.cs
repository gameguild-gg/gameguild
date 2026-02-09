using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to get active subscription for tenant
/// </summary>
public sealed record GetActiveTenantSubscriptionQuery(Guid TenantId) : IQuery<Subscription?>;
