using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.Events;

/// <summary>
/// Domain event raised when a subscription is created
/// </summary>
public class SubscriptionCreatedEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }
    public Guid SubscriptionPlanId { get; }
    public DateTime StartDate { get; }
    public DateTime? TrialEndDate { get; }

    public SubscriptionCreatedEvent(Guid subscriptionId, Guid userId, Guid subscriptionPlanId, DateTime startDate, DateTime? trialEndDate = null)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        SubscriptionPlanId = subscriptionPlanId;
        StartDate = startDate;
        TrialEndDate = trialEndDate;
    }
}

/// <summary>
/// Domain event raised when a subscription is activated
/// </summary>
public class SubscriptionActivatedEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }

    public SubscriptionActivatedEvent(Guid subscriptionId, Guid userId)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
    }
}

/// <summary>
/// Domain event raised when a subscription is cancelled
/// </summary>
public class SubscriptionCancelledEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }
    public string CancellationReason { get; }
    public SubscriptionStatus PreviousStatus { get; }

    public SubscriptionCancelledEvent(Guid subscriptionId, Guid userId, string cancellationReason, SubscriptionStatus previousStatus)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        CancellationReason = cancellationReason;
        PreviousStatus = previousStatus;
    }
}

/// <summary>
/// Domain event raised when a subscription is suspended
/// </summary>
public class SubscriptionSuspendedEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }
    public string? Reason { get; }

    public SubscriptionSuspendedEvent(Guid subscriptionId, Guid userId, string? reason = null)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        Reason = reason;
    }
}

/// <summary>
/// Domain event raised when a subscription trial starts
/// </summary>
public class SubscriptionTrialStartedEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }
    public DateTime TrialEndDate { get; }

    public SubscriptionTrialStartedEvent(Guid subscriptionId, Guid userId, DateTime trialEndDate)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        TrialEndDate = trialEndDate;
    }
}

/// <summary>
/// Domain event raised when a subscription trial ends
/// </summary>
public class SubscriptionTrialEndedEvent : DomainEventBase
{
    public Guid SubscriptionId { get; }
    public Guid UserId { get; }
    public bool ConvertedToPaid { get; }

    public SubscriptionTrialEndedEvent(Guid subscriptionId, Guid userId, bool convertedToPaid)
    {
        SubscriptionId = subscriptionId;
        UserId = userId;
        ConvertedToPaid = convertedToPaid;
    }
}
