using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Domain event raised when a subscription plan is created
/// </summary>
public class PlanCreatedEvent(Guid planId, string name, decimal monthlyPriceInCents) : DomainEvent
{
    public Guid PlanId { get; } = planId;

    public string Name { get; } = name;

    public decimal MonthlyPriceInCents { get; } = monthlyPriceInCents;
}
