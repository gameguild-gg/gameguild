using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query to get subscription by external ID
/// </summary>
public record GetSubscriptionByExternalIdQuery(string ExternalId) : IQuery<Subscription?>;

