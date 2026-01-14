
using GameGuild.ValueObjects;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Composite service interface for all subscription business operations.
///     Extends focused interfaces for backward compatibility.
/// </summary>
/// <remarks>
///     For new code, prefer injecting the focused interfaces:
///     - <see cref="ISubscriptionLifecycleService"/> for create, activate, cancel, suspend operations
///     - <see cref="ISubscriptionBillingService"/> for payments, renewals, reminders
///     - <see cref="ISubscriptionQueryService"/> for read-only query operations
///     - <see cref="ISubscriptionExternalIdService"/> for payment provider integration
/// </remarks>
[Obsolete("Prefer using focused interfaces (ISubscriptionLifecycleService, ISubscriptionBillingService, ISubscriptionQueryService, ISubscriptionExternalIdService) for new code. This composite interface is maintained for backward compatibility.")]
public interface ISubscriptionService 
    : ISubscriptionLifecycleService, 
      ISubscriptionBillingService, 
      ISubscriptionQueryService, 
      ISubscriptionExternalIdService
{
}
