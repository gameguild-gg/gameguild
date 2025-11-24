using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Queries;

/// <summary>
///     Query to check if tenant has active subscription
/// </summary>
public abstract record HasActiveSubscriptionQuery(Guid TenantId) : IQuery<bool>;
