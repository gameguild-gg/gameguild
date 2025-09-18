using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Models;


namespace GameGuild.Modules.Subscriptions.Services;

/// <summary>
/// Service implementation for managing user subscriptions with billing and lifecycle operations
/// </summary>
/// <remarks>
/// This service provides high-level operations for subscription management including
/// creation, cancellation, renewal, payment processing, and access validation.
/// </remarks>
public class SubscriptionService(ApplicationDbContext context) : ISubscriptionService {

  /// <summary>
  /// Retrieves all subscriptions for a specific user ordered by creation date
  /// </summary>
  /// <param name="userId">The unique identifier of the user</param>
  /// <returns>Collection of user subscriptions with plan details</returns>
  public async Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId) {
    // Include subscription plan details and order by most recent first
    return await context.UserSubscriptions
      .Where(s => s.UserId == userId)
      .Include(s => s.SubscriptionPlan)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync();
  }

  /// <summary>
  /// Retrieves the active subscription for a user, if any
  /// </summary>
  /// <param name="userId">The unique identifier of the user</param>
  /// <returns>Active subscription with plan details, or null if none found</returns>
  public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId) {
    // Find active subscription with plan information
    return await context.UserSubscriptions
      .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
      .Include(s => s.SubscriptionPlan)
      .FirstOrDefaultAsync();
  }

  /// <summary>
  /// Retrieves a subscription by its unique identifier with full details
  /// </summary>
  /// <param name="id">The unique identifier of the subscription</param>
  /// <returns>Subscription with plan and user details, or null if not found</returns>
  public async Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id) {
    // Include both subscription plan and user details for complete context
    return await context.UserSubscriptions
      .Include(s => s.SubscriptionPlan)
      .Include(s => s.User)
      .FirstOrDefaultAsync(s => s.Id == id);
  }

  /// <summary>
  /// Retrieves paginated subscriptions with optional status filtering
  /// </summary>
  /// <param name="skip">Number of records to skip for pagination</param>
  /// <param name="take">Number of records to take (max page size)</param>
  /// <param name="status">Optional status filter for subscriptions</param>
  /// <returns>Paginated collection of subscriptions with full details</returns>
  public async Task<IEnumerable<UserSubscription>> GetSubscriptionsAsync(int skip = 0, int take = 50, SubscriptionStatus? status = null) {
    var query = context.UserSubscriptions
      .Include(s => s.SubscriptionPlan)
      .Include(s => s.User)
      .AsQueryable();

    // Apply status filter if specified
    if (status.HasValue) {
      query = query.Where(s => s.Status == status.Value);
    }

    // Apply pagination and order by most recent first
    return await query
      .Skip(skip)
      .Take(take)
      .OrderByDescending(s => s.CreatedAt)
      .ToListAsync();
  }

  /// <summary>
  /// Creates a new subscription for a user with specified configuration
  /// </summary>
  /// <param name="userId">The unique identifier of the user</param>
  /// <param name="createDto">Subscription creation configuration</param>
  /// <returns>The created subscription with full details</returns>
  public async Task<UserSubscription> CreateSubscriptionAsync(Guid userId, CreateSubscriptionDto createDto) {
    // Create subscription with default monthly billing cycle
    var subscription = new UserSubscription {
      UserId = userId,
      SubscriptionPlanId = createDto.SubscriptionPlanId,
      Status = SubscriptionStatus.Active,
      ExternalSubscriptionId = createDto.ExternalSubscriptionId,
      CurrentPeriodStart = DateTime.UtcNow,
      CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1), // Default to monthly billing
      TrialEndsAt = createDto.TrialEndsAt,
      // Set next billing to trial end or monthly cycle
      NextBillingAt = createDto.TrialEndsAt ?? DateTime.UtcNow.AddMonths(1),
    };

    // Persist the new subscription
    context.UserSubscriptions.Add(subscription);
    await context.SaveChangesAsync();

    // Return subscription with full navigation properties loaded
    return await GetSubscriptionByIdAsync(subscription.Id) ?? subscription;
  }

  /// <summary>
  /// Cancels a user's subscription with immediate effect
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="userId">The unique identifier of the user (for authorization)</param>
  /// <returns>The cancelled subscription, or null if not found or unauthorized</returns>
  public async Task<UserSubscription?> CancelSubscriptionAsync(Guid subscriptionId, Guid userId) {
    // Find subscription owned by the specified user
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription is null) return null;

    // Apply cancellation with end-of-period termination
    subscription.Status = SubscriptionStatus.Cancelled;
    subscription.CanceledAt = DateTime.UtcNow;
    subscription.EndsAt = subscription.CurrentPeriodEnd; // Honor current billing period

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  /// <summary>
  /// Resumes a cancelled subscription by reactivating billing
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="userId">The unique identifier of the user (for authorization)</param>
  /// <returns>The resumed subscription, or null if not found or unauthorized</returns>
  public async Task<UserSubscription?> ResumeSubscriptionAsync(Guid subscriptionId, Guid userId) {
    // Find cancelled subscription owned by the specified user
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription is null) return null;

    // Reactivate subscription with new billing cycle
    subscription.Status = SubscriptionStatus.Active;
    subscription.CanceledAt = null;
    subscription.EndsAt = null;
    subscription.NextBillingAt = DateTime.UtcNow.AddMonths(1); // Resume monthly billing

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  /// <summary>
  /// Updates the payment method for a user's subscription
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="userId">The unique identifier of the user (for authorization)</param>
  /// <param name="paymentMethodId">The unique identifier of the new payment method</param>
  /// <returns>The updated subscription, or null if not found or unauthorized</returns>
  /// <remarks>PaymentMethodId field needs to be added to UserSubscription model</remarks>
  public async Task<UserSubscription?> UpdatePaymentMethodAsync(Guid subscriptionId, Guid userId, Guid paymentMethodId) {
    // Find subscription owned by the specified user
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription is null) return null;

    // TODO: Add PaymentMethodId property to UserSubscription model
    // subscription.PaymentMethodId = paymentMethodId;

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  /// <summary>
  /// Renews a subscription by advancing the billing period and recording payment
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <returns>The renewed subscription, or null if not found</returns>
  public async Task<UserSubscription?> RenewSubscriptionAsync(Guid subscriptionId) {
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription is null) return null;

    // Advance billing period by one month (default cycle)
    subscription.CurrentPeriodStart = subscription.CurrentPeriodEnd;
    subscription.CurrentPeriodEnd = subscription.CurrentPeriodEnd.AddMonths(1);
    subscription.LastPaymentAt = DateTime.UtcNow;
    subscription.NextBillingAt = subscription.CurrentPeriodEnd;

    await context.SaveChangesAsync();

    return subscription;
  }

  /// <summary>
  /// Checks if a user has any active subscription that hasn't expired
  /// </summary>
  /// <param name="userId">The unique identifier of the user</param>
  /// <returns>True if user has active subscription, false otherwise</returns>
  public async Task<bool> IsSubscriptionActiveAsync(Guid userId) {
    // Check for active status and unexpired period
    return await context.UserSubscriptions.AnyAsync(s =>
        s.UserId == userId &&
        s.Status == SubscriptionStatus.Active &&
        s.CurrentPeriodEnd > DateTime.UtcNow
    );
  }

  /// <summary>
  /// Checks if a user has access to a specific product through their subscription
  /// </summary>
  /// <param name="userId">The unique identifier of the user</param>
  /// <param name="productId">The unique identifier of the product</param>
  /// <returns>True if user has access to the product, false otherwise</returns>
  public async Task<bool> HasAccessToProductAsync(Guid userId, Guid productId) {
    // Check if user has active subscription that includes this product
    return await context.UserSubscriptions
        .Include(s => s.UserProducts)
        .AnyAsync(s =>
            s.UserId == userId &&
            s.Status == SubscriptionStatus.Active &&
            s.CurrentPeriodEnd > DateTime.UtcNow &&
            s.UserProducts.Any(up => up.ProductId == productId)
        );
  }

  /// <summary>
  /// Updates the external subscription identifier for payment provider integration
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="externalId">The external subscription ID from payment provider</param>
  /// <returns>The updated subscription, or null if not found</returns>
  public async Task<UserSubscription?> UpdateExternalSubscriptionIdAsync(Guid subscriptionId, string externalId) {
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription is null) return null;

    // Link subscription to external payment provider
    subscription.ExternalSubscriptionId = externalId;
    await context.SaveChangesAsync();

    return subscription;
  }

  /// <summary>
  /// Processes a successful payment for a subscription
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="amount">The payment amount processed</param>
  /// <param name="currency">The currency of the payment</param>
  /// <returns>The updated subscription, or null if not found</returns>
  public async Task<UserSubscription?> ProcessPaymentAsync(Guid subscriptionId, decimal amount, string currency) {
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription is null) return null;

    // Record successful payment and ensure active status
    subscription.LastPaymentAt = DateTime.UtcNow;
    subscription.Status = SubscriptionStatus.Active;

    await context.SaveChangesAsync();

    return subscription;
  }

  /// <summary>
  /// Handles payment failure for a subscription by updating status
  /// </summary>
  /// <param name="subscriptionId">The unique identifier of the subscription</param>
  /// <param name="reason">The reason for payment failure</param>
  /// <returns>The updated subscription, or null if not found</returns>
  /// <remarks>Consider adding PaymentFailureReason field to UserSubscription model</remarks>
  public async Task<UserSubscription?> HandlePaymentFailureAsync(Guid subscriptionId, string reason) {
    var subscription = await context.UserSubscriptions
      .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription is null) return null;

    // Mark subscription as past due for payment retry logic
    subscription.Status = SubscriptionStatus.PastDue;
    // TODO: Add PaymentFailureReason field to store failure details

    await context.SaveChangesAsync();

    return subscription;
  }
}
