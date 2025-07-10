using GameGuild.Common;
using GameGuild.Modules.Subscriptions.Models;

namespace GameGuild.Modules.Subscriptions.Services;

/// <summary>
/// Service interface for managing user subscriptions
/// </summary>
public interface ISubscriptionService
{
    // User subscription management
    Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId);
    Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId);
    Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id);
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
