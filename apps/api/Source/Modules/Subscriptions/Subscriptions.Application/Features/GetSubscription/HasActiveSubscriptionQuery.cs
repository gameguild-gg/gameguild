using MediatR;

namespace GameGuild.Modules.Subscriptions.Features.GetSubscription;

/// <summary>
///     Query to check if tenant has active subscription
/// </summary>
public record HasActiveSubscriptionQuery(Guid TenantId) : IQuery<bool>;

