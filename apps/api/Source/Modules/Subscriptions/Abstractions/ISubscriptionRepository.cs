using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Abstractions;

/// <summary>
/// Repository interface for subscription data access
/// </summary>
public interface ISubscriptionRepository : IRepository<UserSubscription, Guid>
{
    /// <summary>
    /// Gets all subscriptions for a user
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the active subscription for a user
    /// </summary>
    Task<UserSubscription?> GetActiveUserSubscriptionAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all subscriptions for a plan
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetByPlanIdAsync(Guid planId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions by status
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions expiring soon (within specified days)
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetExpiringSoonAsync(int withinDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions requiring billing processing
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetPendingBillingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscriptions with trials ending soon
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetTrialsEndingSoonAsync(int withinDays, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscription by external subscription ID
    /// </summary>
    Task<UserSubscription?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets subscription count by status
    /// </summary>
    Task<int> GetCountByStatusAsync(SubscriptionStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active subscriptions count for a user
    /// </summary>
    Task<int> GetActiveSubscriptionCountAsync(Guid userId, CancellationToken cancellationToken = default);
}