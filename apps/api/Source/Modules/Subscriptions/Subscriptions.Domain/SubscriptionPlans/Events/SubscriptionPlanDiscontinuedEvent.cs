using GameGuild.CQRS;

namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Events;

/// <summary>
///     Domain event raised when a subscription plan is discontinued
/// </summary>
public class SubscriptionPlanDiscontinuedEvent : DomainEvent
{
    public SubscriptionPlanDiscontinuedEvent(Guid planId, string name)
    {
        PlanId = planId;
        Name = name;
    }

    public Guid PlanId { get; }

    public string Name { get; }
}

