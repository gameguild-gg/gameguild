using GameGuild.CQRS;

namespace GameGuild.Identity.Authentication;

public sealed record UpdateConditionalPolicyPriorityCommand : ICommand
{
    public Guid PolicyId { get; set; }

    public int NewPriority { get; init; }
}
