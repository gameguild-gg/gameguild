using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Events;

/// <summary>
///     Domain event raised when a subscription plan is changed
/// </summary>
public class SubscriptionPlanChangedEvent : DomainEvent {
    public SubscriptionPlanChangedEvent(Guid planId, string oldName, string newName)
      : base(planId, "SubscriptionPlan") {
        PlanId = planId;
        OldName = oldName;
        NewName = newName;
    }

    public Guid PlanId { get; }

    public string OldName { get; }

    public string NewName { get; }
}

