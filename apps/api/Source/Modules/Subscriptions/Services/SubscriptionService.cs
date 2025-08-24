using GameGuild.Common;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Models;


namespace GameGuild.Modules.Subscriptions.Services;

/// <summary>
/// Service implementation for managing user subscriptions
/// </summary>
public class SubscriptionService(ApplicationDbContext context) : ISubscriptionService {
  public async Task<IEnumerable<UserSubscription>> GetUserSubscriptionsAsync(Guid userId) {
    return await context.UserSubscriptions
                        .Where(s => s.UserId == userId)
                        .Include(s => s.SubscriptionPlan)
                        .OrderByDescending(s => s.CreatedAt)
                        .ToListAsync();
  }

  public async Task<UserSubscription?> GetActiveSubscriptionAsync(Guid userId) {
    return await context.UserSubscriptions
                        .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
                        .Include(s => s.SubscriptionPlan)
                        .FirstOrDefaultAsync();
  }

  public async Task<UserSubscription?> GetSubscriptionByIdAsync(Guid id) {
    return await context.UserSubscriptions
                        .Include(s => s.SubscriptionPlan)
                        .Include(s => s.User)
                        .FirstOrDefaultAsync(s => s.Id == id);
  }

  public async Task<IEnumerable<UserSubscription>> GetSubscriptionsAsync(int skip = 0, int take = 50, SubscriptionStatus? status = null) {
    var query = context.UserSubscriptions
                       .Include(s => s.SubscriptionPlan)
                       .Include(s => s.User)
                       .AsQueryable();

    if (status.HasValue) query = query.Where(s => s.Status == status.Value);

    return await query
                 .Skip(skip)
                 .Take(take)
                 .OrderByDescending(s => s.CreatedAt)
                 .ToListAsync();
  }

  public async Task<UserSubscription> CreateSubscriptionAsync(Guid userId, CreateSubscriptionDto createDto) {
    var subscription = new UserSubscription {
      UserId = userId,
      SubscriptionPlanId = createDto.SubscriptionPlanId,
      Status = SubscriptionStatus.Active,
      ExternalSubscriptionId = createDto.ExternalSubscriptionId,
      CurrentPeriodStart = DateTime.UtcNow,
      CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1), // Default to monthly
      TrialEndsAt = createDto.TrialEndsAt,
      NextBillingAt = createDto.TrialEndsAt ?? DateTime.UtcNow.AddMonths(1),
    };

    context.UserSubscriptions.Add(subscription);
    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscription.Id) ?? subscription;
  }

  public async Task<UserSubscription?> CancelSubscriptionAsync(Guid subscriptionId, Guid userId) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription == null) return null;

    subscription.Status = SubscriptionStatus.Canceled;
    subscription.CanceledAt = DateTime.UtcNow;
    subscription.EndsAt = subscription.CurrentPeriodEnd;

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  public async Task<UserSubscription?> ResumeSubscriptionAsync(Guid subscriptionId, Guid userId) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription == null) return null;

    subscription.Status = SubscriptionStatus.Active;
    subscription.CanceledAt = null;
    subscription.EndsAt = null;
    subscription.NextBillingAt = DateTime.UtcNow.AddMonths(1);

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  public async Task<UserSubscription?> UpdatePaymentMethodAsync(Guid subscriptionId, Guid userId, Guid paymentMethodId) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId);

    if (subscription == null) return null;

    // Note: You'll need to add PaymentMethodId to UserSubscription model if needed
    // subscription.PaymentMethodId = paymentMethodId;

    await context.SaveChangesAsync();

    return await GetSubscriptionByIdAsync(subscriptionId);
  }

  public async Task<UserSubscription?> RenewSubscriptionAsync(Guid subscriptionId) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription == null) return null;

    subscription.CurrentPeriodStart = subscription.CurrentPeriodEnd;
    subscription.CurrentPeriodEnd = subscription.CurrentPeriodEnd.AddMonths(1);
    subscription.LastPaymentAt = DateTime.UtcNow;
    subscription.NextBillingAt = subscription.CurrentPeriodEnd;

    await context.SaveChangesAsync();

    return subscription;
  }

  public async Task<bool> IsSubscriptionActiveAsync(Guid userId) {
    return await context.UserSubscriptions
                        .AnyAsync(s => s.UserId == userId &&
                                       s.Status == SubscriptionStatus.Active &&
                                       s.CurrentPeriodEnd > DateTime.UtcNow
                        );
  }

  public async Task<bool> HasAccessToProductAsync(Guid userId, Guid productId) {
    // Check if user has active subscription that includes this product
    return await context.UserSubscriptions
                        .Include(s => s.UserProducts)
                        .AnyAsync(s => s.UserId == userId &&
                                       s.Status == SubscriptionStatus.Active &&
                                       s.CurrentPeriodEnd > DateTime.UtcNow &&
                                       s.UserProducts.Any(up => up.ProductId == productId)
                        );
  }

  public async Task<UserSubscription?> UpdateExternalSubscriptionIdAsync(Guid subscriptionId, string externalId) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription == null) return null;

    subscription.ExternalSubscriptionId = externalId;
    await context.SaveChangesAsync();

    return subscription;
  }

  public async Task<UserSubscription?> ProcessPaymentAsync(Guid subscriptionId, decimal amount, string currency) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription == null) return null;

    subscription.LastPaymentAt = DateTime.UtcNow;
    subscription.Status = SubscriptionStatus.Active;

    await context.SaveChangesAsync();

    return subscription;
  }

  public async Task<UserSubscription?> HandlePaymentFailureAsync(Guid subscriptionId, string reason) {
    var subscription = await context.UserSubscriptions
                                    .FirstOrDefaultAsync(s => s.Id == subscriptionId);

    if (subscription == null) return null;

    subscription.Status = SubscriptionStatus.PastDue;
    // You might want to add a PaymentFailureReason field to the model

    await context.SaveChangesAsync();

    return subscription;
  }
}
