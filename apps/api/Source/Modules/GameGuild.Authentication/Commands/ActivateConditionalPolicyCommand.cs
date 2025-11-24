using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record ActivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
