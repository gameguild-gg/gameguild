using GameGuild.CQRS;

namespace GameGuild.Authentication.Commands;

public record DeactivateConditionalPolicyCommand : ICommand
{
    public Guid PolicyId { get; init; }
}
