using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Query to check if tenant has active subscription
/// </summary>
public abstract record HasActiveSubscriptionQuery(Guid TenantId) : IQuery<bool>;
