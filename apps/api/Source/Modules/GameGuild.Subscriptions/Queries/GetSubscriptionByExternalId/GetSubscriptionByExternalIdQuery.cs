using GameGuild.CQRS;
using GameGuild.Subscriptions.Entities;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query to get subscription by external ID
/// </summary>
public record GetSubscriptionByExternalIdQuery(string ExternalId) : IQuery<Subscription?>;
