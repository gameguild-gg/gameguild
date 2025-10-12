using GameGuild.CQRS;
using GameGuild.Modules.Subscriptions.Entities;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query to get subscription by ID
/// </summary>
public record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<Subscription?>;

