using GameGuild.CQRS;

namespace GameGuild.Subscriptions.Events;

/// <summary>
///     Domain event raised when a subscription plan is discontinued
/// </summary>
public class PlanDiscontinuedEvent(Guid planId, string name) : DomainEvent
{
    public Guid PlanId { get; } = planId;

    public string Name { get; } = name;
}
