using GameGuild.Modules.Subscriptions.Models;


namespace GameGuild.Modules.Subscriptions.Services;

/// <summary> 
/// Service interface for managing user subscriptions throughout their lifecycle.
/// Provides operations for creation, cancellation, billing, and subscription status management.
/// Integrates with external payment providers and handles business logic for subscription workflows.
/// </summary>
public interface ISubscriptionService {
  // User subscription management

  /// <summary>
  /// Retrieves all subscriptions for a specific user.
  /// </summary>
  /// <param name="userId">The ID of the user whose subscriptions to retrieve</param>
  /// <returns>Collection of user subscriptions including historical and current subscriptions</returns>
  Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId);

  /// <summary>
  /// Gets the currently active subscription for a user.
  /// </summary>
  /// <param name="userId">The ID of the user</param>
  /// <returns>The active subscription or null if no active subscription exists</returns>
  Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId);

  /// <summary>
  /// Retrieves a specific subscription by its ID.
  /// </summary>
  /// <param name="id">The subscription ID</param>
  /// <returns>The subscription or null if not found</returns>
  Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id);

  /// <summary>
  /// Retrieves subscriptions with pagination and optional status filtering.
  /// </summary>
  /// <param name="skip">Number of records to skip for pagination</param>
  /// <param name="take">Number of records to take for pagination</param>
  /// <param name="status">Optional status filter</param>
  /// <returns>Paginated collection of subscriptions</returns>
  Task<IEnumerable<UserSubscription>> GetSubscriptionsAsync(int skip = 0, int take = 50, SubscriptionStatus? status = null);

  // Subscription lifecycle
  Task<UserSubscription> CreateSubscriptionAsync(Guid userId, CreateSubscriptionDto createDto);

  Task<UserSubscription?> CancelSubscriptionAsync(Guid subscriptionId, Guid userId);

  Task<UserSubscription?> ResumeSubscriptionAsync(Guid subscriptionId, Guid userId);

  Task<UserSubscription?> UpdatePaymentMethodAsync(Guid subscriptionId, Guid userId, Guid paymentMethodId);

  // Billing and renewals
  Task<UserSubscription?> RenewSubscriptionAsync(Guid subscriptionId);

  Task<bool> IsSubscriptionActiveAsync(Guid userId);

  Task<bool> HasAccessToProductAsync(Guid userId, Guid productId);

  // External payment provider integration
  Task<UserSubscription?> UpdateExternalSubscriptionIdAsync(Guid subscriptionId, string externalId);

  Task<UserSubscription?> ProcessPaymentAsync(Guid subscriptionId, decimal amount, string currency);

  Task<UserSubscription?> HandlePaymentFailureAsync(Guid subscriptionId, string reason);
}
