using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query to get subscription by ID
/// </summary>
public record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<Subscription?>;
