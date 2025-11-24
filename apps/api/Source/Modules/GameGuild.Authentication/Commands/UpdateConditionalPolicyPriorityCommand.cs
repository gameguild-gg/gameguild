using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record UpdateConditionalPolicyPriorityCommand : ICommand
{
    public Guid PolicyId { get; set; }

    public int NewPriority { get; init; }
}
