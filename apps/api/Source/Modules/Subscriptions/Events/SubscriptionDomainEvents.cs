using GameGuild.CQRS;


namespace GameGuild.Modules.Subscriptions.Events;

/// <summary> 
/// Domain event raised when a new subscription is created for a user.
/// Triggers integration workflows like welcome emails, analytics tracking, and external system notifications.
/// Contains all essential information about the new subscription for downstream processing.
/// </summary>
public class SubscriptionCreatedEvent : DomainEventBase {
  public SubscriptionCreatedEvent(Guid subscriptionId, Guid userId, Guid subscriptionPlanId, DateTime startDate, DateTime? trialEndDate = null) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
    SubscriptionPlanId = subscriptionPlanId;
    StartDate = startDate;
    TrialEndDate = trialEndDate;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }

  public Guid SubscriptionPlanId { get; }

  public DateTime StartDate { get; }

  public DateTime? TrialEndDate { get; }
}

/// <summary> 
/// Domain event raised when a subscription transitions from inactive to active status.
/// This typically occurs after successful payment processing or trial conversion.
/// Triggers feature access updates and billing system notifications.
/// </summary>
public class SubscriptionActivatedEvent : DomainEventBase {
  public SubscriptionActivatedEvent(Guid subscriptionId, Guid userId) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }
}

/// <summary> 
/// Domain event raised when a subscription is cancelled by user or system action.
/// Contains cancellation reason and previous status for analytics and retention workflows.
/// Triggers cleanup processes, feedback collection, and potential win-back campaigns.
/// </summary>
public class SubscriptionCancelledEvent : DomainEventBase {
  public SubscriptionCancelledEvent(Guid subscriptionId, Guid userId, string cancellationReason, SubscriptionStatus previousStatus) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
    CancellationReason = cancellationReason;
    PreviousStatus = previousStatus;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }

  public string CancellationReason { get; }

  public SubscriptionStatus PreviousStatus { get; }
}

/// <summary> Domain event raised when a subscription is suspended </summary>
public class SubscriptionSuspendedEvent : DomainEventBase {
  public SubscriptionSuspendedEvent(Guid subscriptionId, Guid userId, string? reason = null) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
    Reason = reason;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }

  public string? Reason { get; }
}

/// <summary> Domain event raised when a subscription trial starts </summary>
public class SubscriptionTrialStartedEvent : DomainEventBase {
  public SubscriptionTrialStartedEvent(Guid subscriptionId, Guid userId, DateTime trialEndDate) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
    TrialEndDate = trialEndDate;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }

  public DateTime TrialEndDate { get; }
}

/// <summary> Domain event raised when a subscription trial ends </summary>
public class SubscriptionTrialEndedEvent : DomainEventBase {
  public SubscriptionTrialEndedEvent(Guid subscriptionId, Guid userId, bool convertedToPaid) : base(subscriptionId, "UserSubscription") {
    SubscriptionId = subscriptionId;
    UserId = userId;
    ConvertedToPaid = convertedToPaid;
  }

  public Guid SubscriptionId { get; }

  public Guid UserId { get; }

  public bool ConvertedToPaid { get; }
}
