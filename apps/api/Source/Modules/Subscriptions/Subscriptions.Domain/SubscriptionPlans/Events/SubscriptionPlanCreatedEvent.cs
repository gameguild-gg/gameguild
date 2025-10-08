using MediatR;

using MediatR;

namespace GameGuild.Modules.Subscriptions.SubscriptionPlans.Events;

/// <summary>
///     Domain event raised when a subscription plan is created
/// </summary>
public class SubscriptionPlanCreatedEvent : DomainEvent
{
    public SubscriptionPlanCreatedEvent(Guid planId, string name, decimal monthlyPriceInCents)
    {
        PlanId = planId;
        Name = name;
        MonthlyPriceInCents = monthlyPriceInCents;
    }

    public Guid PlanId { get; }

    public string Name { get; }

    public decimal MonthlyPriceInCents { get; }
}

