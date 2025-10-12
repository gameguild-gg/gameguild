using GameGuild.CQRS;
using GameGuild.Database;
using GameGuild.Modules.Subscriptions.Abstractions;
using GameGuild.Modules.Subscriptions.Models;


namespace GameGuild.Modules.Subscriptions.Commands.CreateSubscription;

/// <summary>
/// Command handler for creating new user subscriptions with validation and billing setup
/// </summary>
/// <remarks>
/// This handler validates user and plan existence, configures billing cycles, 
/// handles trial periods, and creates the subscription entity with proper domain events.
/// </remarks>
public class CreateSubscriptionCommandHandler(ISubscriptionRepository subscriptionRepository, ApplicationDbContext context) : ICommandHandler<CreateSubscriptionCommand, Guid> {
  private readonly ApplicationDbContext _context = context;
  private readonly ISubscriptionRepository _subscriptionRepository = subscriptionRepository;

  /// <summary>
  /// Handles the subscription creation command with validation and persistence
  /// </summary>
  /// <param name="request">The subscription creation request</param>
  /// <param name="cancellationToken">Cancellation token for async operations</param>
  /// <returns>The unique identifier of the created subscription</returns>
  /// <exception cref="ArgumentException">Thrown when user or subscription plan is not found</exception>
  public async Task<Guid> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken) {
    // Validate that the user exists before creating subscription
    var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId, cancellationToken);
    if (!userExists) {
      throw new ArgumentException($"User with ID {request.UserId} not found");
    }

    // Validate that the subscription plan exists and is available
    var planExists = await _context.ProductSubscriptionPlans.AnyAsync(p => p.Id == request.SubscriptionPlanId, cancellationToken);
    if (!planExists) {
      throw new ArgumentException($"Subscription plan with ID {request.SubscriptionPlanId} not found");
    }

    // Use provided start date or default to current time
    var startDate = request.StartDate ?? DateTime.UtcNow;

    // Calculate trial end date if trial period is specified
    DateTime? trialEndDate = request.TrialDays.HasValue ? startDate.AddDays(request.TrialDays.Value) : null;

    // Create money object with proper currency handling
    var money = new Money(request.Amount, request.Currency);

    // Create subscription using factory method with domain events
    var subscription = UserSubscription.Create(
        request.UserId,
        request.SubscriptionPlanId,
        request.BillingCycle,
        money,
        startDate,
        trialEndDate
    );

    // Persist the subscription and commit changes
    _ = await _subscriptionRepository.AddAsync(subscription, cancellationToken);
    _ = await _subscriptionRepository.SaveChangesAsync(cancellationToken);

    return subscription.Id;
  }
}
