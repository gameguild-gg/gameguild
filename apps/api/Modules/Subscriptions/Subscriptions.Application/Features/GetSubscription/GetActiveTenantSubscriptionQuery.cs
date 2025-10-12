using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query to get active subscription for tenant
/// </summary>
public record GetActiveTenantSubscriptionQuery(Guid TenantId) : IQuery<Subscription?>;

