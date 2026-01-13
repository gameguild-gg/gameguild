using GameGuild.CQRS;

namespace GameGuild.Commerce.Subscriptions;

/// <summary>
///     Domain event raised when a subscription plan is modified
/// </summary>
public class PlanModifiedEvent(Guid planId, string oldName, string newName) : DomainEvent
{
    public Guid PlanId { get; } = planId;

    public string OldName { get; } = oldName;

    public string NewName { get; } = newName;
}
